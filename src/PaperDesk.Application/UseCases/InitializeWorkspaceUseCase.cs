using PaperDesk.Application.Abstractions;

namespace PaperDesk.Application.UseCases;

public sealed class InitializeWorkspaceUseCase
{
    private readonly IFolderWatcherService _folderWatcherService;

    public InitializeWorkspaceUseCase(IFolderWatcherService folderWatcherService)
    {
        _folderWatcherService = folderWatcherService;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken) =>
        _folderWatcherService.StartAsync(Array.Empty<PaperDesk.Domain.Entities.WatchedFolder>(), cancellationToken);
}
