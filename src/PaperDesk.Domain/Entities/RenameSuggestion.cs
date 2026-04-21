using PaperDesk.Domain.Enums;

namespace PaperDesk.Domain.Entities;

public sealed class RenameSuggestion
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid DocumentId { get; init; }

    public required string ProposedFileName { get; init; }

    public string? ProposedDestinationDirectory { get; init; }

    public ConfidenceLevel Confidence { get; init; } = ConfidenceLevel.Medium;

    public string Reason { get; init; } = "Heuristic extraction";

    public bool IsApproved { get; set; }

    public bool IsSkipped { get; set; }
}
