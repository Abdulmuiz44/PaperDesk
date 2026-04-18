using System.Collections.Concurrent;
using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Enums;
using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Infrastructure.Services;

public sealed class FileProcessingQueue : IFileProcessingQueue
{
    private readonly ConcurrentDictionary<string, ProcessingItem> items = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim gate = new(1, 1);

    public Task<ProcessingItem> EnqueueAsync(string fullPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedPath = Path.GetFullPath(fullPath);
        var item = items.AddOrUpdate(
            normalizedPath,
            path => new ProcessingItem { FullPath = path },
            (_, existing) => existing.Status is ProcessingStatus.Completed
                ? new ProcessingItem { FullPath = normalizedPath }
                : existing with { Status = ProcessingStatus.Queued, QueuedUtc = DateTimeOffset.UtcNow });

        return Task.FromResult(item);
    }

    public async Task<ProcessingItem?> TryDequeueAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var next = items.Values
                .Where(item => item.Status == ProcessingStatus.Queued)
                .OrderBy(item => item.QueuedUtc)
                .FirstOrDefault();

            if (next is null)
            {
                return null;
            }

            var processing = next with
            {
                Status = ProcessingStatus.Processing,
                AttemptCount = next.AttemptCount + 1
            };
            items[processing.FullPath] = processing;
            return processing;
        }
        finally
        {
            gate.Release();
        }
    }

    public Task MarkCompletedAsync(string fullPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UpdateStatus(fullPath, ProcessingStatus.Completed, null);
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(string fullPath, string error, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UpdateStatus(fullPath, ProcessingStatus.Failed, error);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<ProcessingItem> Snapshot()
        => items.Values.OrderBy(item => item.QueuedUtc).ToArray();

    private void UpdateStatus(string fullPath, ProcessingStatus status, string? error)
    {
        var normalizedPath = Path.GetFullPath(fullPath);
        items.AddOrUpdate(
            normalizedPath,
            path => new ProcessingItem { FullPath = path, Status = status, LastError = error },
            (_, existing) => existing with { Status = status, LastError = error });
    }
}
