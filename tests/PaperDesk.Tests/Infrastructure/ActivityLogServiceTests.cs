using PaperDesk.Domain.Enums;
using PaperDesk.Domain.ValueObjects;
using PaperDesk.Infrastructure.Logging;

namespace PaperDesk.Tests.Infrastructure;

public sealed class ActivityLogServiceTests
{
    [Fact]
    public async Task WriteAsyncRecordsPhaseOneActivityEvents()
    {
        var log = new ActivityLogService();

        await log.WriteAsync(new ActivityEvent(ActivityType.FileQueued, "Queued", "a.pdf"), CancellationToken.None);
        await log.WriteAsync(new ActivityEvent(ActivityType.FileAnalyzed, "Analyzed", "a.pdf"), CancellationToken.None);
        await log.WriteAsync(new ActivityEvent(ActivityType.FileSkipped, "Skipped", "b.exe"), CancellationToken.None);
        await log.WriteAsync(new ActivityEvent(ActivityType.Failure, "Failed", "c.pdf"), CancellationToken.None);

        var snapshot = log.Snapshot();

        Assert.Contains(snapshot, item => item.ActivityType == ActivityType.FileQueued);
        Assert.Contains(snapshot, item => item.ActivityType == ActivityType.FileAnalyzed);
        Assert.Contains(snapshot, item => item.ActivityType == ActivityType.FileSkipped);
        Assert.Contains(snapshot, item => item.ActivityType == ActivityType.Failure);
    }
}
