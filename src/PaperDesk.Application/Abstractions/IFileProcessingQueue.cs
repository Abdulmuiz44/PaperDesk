using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Application.Abstractions;

public interface IFileProcessingQueue
{
    Task<ProcessingItem> EnqueueAsync(string fullPath, CancellationToken cancellationToken);

    Task<ProcessingItem?> TryDequeueAsync(CancellationToken cancellationToken);

    Task MarkCompletedAsync(string fullPath, CancellationToken cancellationToken);

    Task MarkFailedAsync(string fullPath, string error, CancellationToken cancellationToken);

    IReadOnlyCollection<ProcessingItem> Snapshot();
}
