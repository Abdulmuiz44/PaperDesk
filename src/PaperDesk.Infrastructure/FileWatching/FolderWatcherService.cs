using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;
using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Infrastructure.FileWatching;

public sealed class FolderWatcherService(
    IWatchedFolderValidator validator,
    IFileProcessingQueue queue,
    IActivityLog activityLog) : IFolderWatcherService, IDisposable
{
    private readonly List<FileSystemWatcher> watchers = [];
    private readonly object syncRoot = new();
    private bool disposed;

    public Task StartAsync(IReadOnlyCollection<WatchedFolder> folders, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StopWatchers();

        foreach (var folder in folders.Where(folder => folder.IsEnabled))
        {
            var validation = validator.Validate(folder);
            if (!validation.IsValid)
            {
                _ = activityLog.WriteAsync(new ActivityEvent(
                    ActivityType.FileSkipped,
                    validation.Error ?? "Watched folder skipped.",
                    folder.Path), cancellationToken);
                continue;
            }

            var watcher = new FileSystemWatcher(Path.GetFullPath(folder.Path))
            {
                IncludeSubdirectories = folder.IsRecursive,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            watcher.Created += (_, args) => QueueIfSupported(args.FullPath, folder);
            watcher.Changed += (_, args) => QueueIfSupported(args.FullPath, folder);
            watcher.Renamed += (_, args) => QueueIfSupported(args.FullPath, folder);
            watcher.Error += (_, args) => _ = activityLog.WriteAsync(new ActivityEvent(
                ActivityType.Failure,
                args.GetException().Message,
                folder.Path), CancellationToken.None);

            lock (syncRoot)
            {
                watchers.Add(watcher);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StopWatchers();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        StopWatchers();
        disposed = true;
    }

    private void QueueIfSupported(string fullPath, WatchedFolder folder)
    {
        if (Directory.Exists(fullPath))
        {
            return;
        }

        var extension = Path.GetExtension(fullPath);
        if (folder.IncludedExtensions.Count > 0 && !folder.IncludedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await activityLog.WriteAsync(new ActivityEvent(ActivityType.FileDetected, $"Detected {Path.GetFileName(fullPath)}", fullPath), CancellationToken.None);
            await queue.EnqueueAsync(fullPath, CancellationToken.None);
            await activityLog.WriteAsync(new ActivityEvent(ActivityType.FileQueued, $"Queued {Path.GetFileName(fullPath)}", fullPath), CancellationToken.None);
        });
    }

    private void StopWatchers()
    {
        lock (syncRoot)
        {
            foreach (var watcher in watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            watchers.Clear();
        }
    }
}
