using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Infrastructure.Logging;

public sealed class ActivityLogService : IActivityLogService
{
    public Task WriteAsync(ActivityLogEntry entry, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<IReadOnlyCollection<ActivityLogEntry>> GetRecentAsync(int maxCount, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<ActivityLogEntry>>(Array.Empty<ActivityLogEntry>());
}
