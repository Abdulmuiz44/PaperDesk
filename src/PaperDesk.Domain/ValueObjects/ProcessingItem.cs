using PaperDesk.Domain.Enums;

namespace PaperDesk.Domain.ValueObjects;

public sealed record ProcessingItem
{
    public required string FullPath { get; init; }

    public ProcessingStatus Status { get; init; } = ProcessingStatus.Queued;

    public int AttemptCount { get; init; }

    public string? LastError { get; init; }

    public DateTimeOffset QueuedUtc { get; init; } = DateTimeOffset.UtcNow;
}
