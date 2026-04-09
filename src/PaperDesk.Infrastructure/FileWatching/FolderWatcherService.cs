using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Infrastructure.FileWatching;

public sealed class FolderWatcherService : IFolderWatcherService
{
    public Task StartAsync(IReadOnlyCollection<WatchedFolder> folders, CancellationToken cancellationToken)
    {
        // Placeholder: real FileSystemWatcher behavior intentionally deferred.
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
