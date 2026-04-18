using System.Collections.Concurrent;
using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Infrastructure.Logging;

public sealed class ActivityLogService : IActivityLogService, IActivityLog
{
    private readonly ConcurrentQueue<ActivityEvent> events = new();

    public Task WriteAsync(ActivityLogEntry entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(new ActivityEvent(
            entry.ActivityType,
            entry.Message,
            DocumentId: entry.DocumentId,
            MetadataJson: entry.MetadataJson,
            OccurredUtc: entry.OccurredUtc));
        return Task.CompletedTask;
    }

    public Task WriteAsync(ActivityEvent activityEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(activityEvent with { OccurredUtc = activityEvent.OccurredUtc ?? DateTimeOffset.UtcNow });
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ActivityLogEntry>> GetRecentAsync(int maxCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var recent = events
            .Reverse()
            .Take(Math.Max(0, maxCount))
            .Select(activityEvent => new ActivityLogEntry
            {
                ActivityType = activityEvent.ActivityType,
                DocumentId = activityEvent.DocumentId,
                Message = activityEvent.Message,
                MetadataJson = activityEvent.MetadataJson,
                OccurredUtc = activityEvent.OccurredUtc ?? DateTimeOffset.UtcNow
            })
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ActivityLogEntry>>(recent);
    }

    public IReadOnlyCollection<ActivityEvent> Snapshot()
        => events.ToArray();
}
