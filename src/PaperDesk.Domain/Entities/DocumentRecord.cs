using PaperDesk.Domain.Enums;

namespace PaperDesk.Domain.Entities;

public sealed class DocumentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string OriginalPath { get; init; }

    public string? CurrentPath { get; set; }

    public DocumentType DocumentType { get; set; } = DocumentType.Unknown;

    public string? ExtractedText { get; set; }

    public ConfidenceLevel OcrConfidence { get; set; } = ConfidenceLevel.Low;

    public string? Sha256Hash { get; set; }

    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

    public DateTimeOffset DiscoveredUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastProcessedUtc { get; set; }
}
