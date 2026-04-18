using PaperDesk.Domain.Enums;

namespace PaperDesk.Domain.ValueObjects;

public sealed record RenameMovePreview(
    string OriginalPath,
    string ProposedPath,
    string ProposedFileName,
    string ProposedDirectory,
    ConfidenceLevel Confidence,
    string Reason);
