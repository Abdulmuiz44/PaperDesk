using PaperDesk.Domain.Enums;

namespace PaperDesk.Domain.Entities;

public sealed class ActivityLogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public ActivityType ActivityType { get; init; }

    public Guid? DocumentId { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? MetadataJson { get; init; }

    public DateTimeOffset OccurredUtc { get; init; } = DateTimeOffset.UtcNow;
}
