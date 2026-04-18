using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Application.Abstractions;

public interface IActivityLog
{
    Task WriteAsync(ActivityEvent activityEvent, CancellationToken cancellationToken);

    IReadOnlyCollection<ActivityEvent> Snapshot();
}
