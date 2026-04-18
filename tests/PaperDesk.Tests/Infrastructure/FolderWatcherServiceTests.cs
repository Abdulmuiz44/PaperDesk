using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;
using PaperDesk.Infrastructure.FileWatching;
using PaperDesk.Infrastructure.Logging;
using PaperDesk.Infrastructure.Services;
using PaperDesk.Infrastructure.Validation;

namespace PaperDesk.Tests.Infrastructure;

public sealed class FolderWatcherServiceTests
{
    [Fact]
    public async Task StartAsyncQueuesSupportedNewFiles()
    {
        using var temp = new TempDirectory();
        var queue = new FileProcessingQueue();
        var log = new ActivityLogService();
        using var watcher = new FolderWatcherService(new WatchedFolderValidator(), queue, log);
        var folder = new WatchedFolder { Path = temp.Path, IncludedExtensions = [".pdf"] };

        await watcher.StartAsync([folder], CancellationToken.None);
        var filePath = Path.Combine(temp.Path, "invoice.pdf");
        await File.WriteAllTextAsync(filePath, "sample");

        var queued = await WaitForAsync(() => queue.Snapshot().Any(item => item.FullPath == filePath));
        await watcher.StopAsync(CancellationToken.None);

        Assert.True(queued);
        Assert.Contains(log.Snapshot(), item => item.ActivityType == ActivityType.FileQueued && item.Path == filePath);
    }

    private static async Task<bool> WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!timeout.IsCancellationRequested)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(100, CancellationToken.None);
        }

        return false;
    }
}
