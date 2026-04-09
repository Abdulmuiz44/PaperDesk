using PaperDesk.Domain.Enums;

namespace PaperDesk.Application.DTOs;

public sealed record RenamePlanDto(
    Guid DocumentId,
    string ProposedFileName,
    string? ProposedDirectory,
    ConfidenceLevel Confidence,
    string Reason);
