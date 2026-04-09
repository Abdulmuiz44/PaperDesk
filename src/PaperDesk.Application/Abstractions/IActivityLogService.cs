using PaperDesk.Domain.Entities;

namespace PaperDesk.Application.Abstractions;

public interface IActivityLogService
{
    Task WriteAsync(ActivityLogEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ActivityLogEntry>> GetRecentAsync(int maxCount, CancellationToken cancellationToken);
}
