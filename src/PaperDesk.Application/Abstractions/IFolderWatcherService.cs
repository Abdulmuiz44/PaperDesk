using PaperDesk.Domain.Entities;

namespace PaperDesk.Application.Abstractions;

public interface IFolderWatcherService
{
    Task StartAsync(IReadOnlyCollection<WatchedFolder> folders, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
