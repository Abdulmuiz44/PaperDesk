using PaperDesk.Domain.Enums;

namespace PaperDesk.Domain.Entities;

public sealed class ProcessingJob
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid DocumentId { get; init; }

    public ProcessingStatus Status { get; set; } = ProcessingStatus.Queued;

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset EnqueuedUtc { get; init; } = DateTimeOffset.UtcNow;
}
