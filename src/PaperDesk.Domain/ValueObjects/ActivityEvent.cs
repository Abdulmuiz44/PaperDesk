using PaperDesk.Domain.Enums;

namespace PaperDesk.Domain.ValueObjects;

public sealed record ActivityEvent(
    ActivityType ActivityType,
    string Message,
    string? Path = null,
    Guid? DocumentId = null,
    string? MetadataJson = null,
    DateTimeOffset? OccurredUtc = null);
