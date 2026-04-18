using PaperDesk.Domain.Enums;
using PaperDesk.Infrastructure.Services;

namespace PaperDesk.Tests.Infrastructure;

public sealed class FileProcessingQueueTests
{
    [Fact]
    public async Task EnqueueAsyncDebouncesRepeatedFileEvents()
    {
        var queue = new FileProcessingQueue();
        var path = Path.Combine(Path.GetTempPath(), "invoice.pdf");

        await queue.EnqueueAsync(path, CancellationToken.None);
        await queue.EnqueueAsync(path, CancellationToken.None);

        Assert.Single(queue.Snapshot());
    }

    [Fact]
    public async Task TryDequeueAsyncMarksItemProcessingAndCompletionIsRecorded()
    {
        var queue = new FileProcessingQueue();
        var path = Path.Combine(Path.GetTempPath(), "invoice.pdf");
        await queue.EnqueueAsync(path, CancellationToken.None);

        var item = await queue.TryDequeueAsync(CancellationToken.None);
        await queue.MarkCompletedAsync(path, CancellationToken.None);

        Assert.NotNull(item);
        Assert.Equal(ProcessingStatus.Processing, item.Status);
        Assert.Equal(ProcessingStatus.Completed, queue.Snapshot().Single().Status);
    }
}
