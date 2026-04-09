namespace PaperDesk.Domain.Entities;

public sealed class DuplicateGroup
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public IReadOnlyCollection<Guid> DocumentIds { get; init; } = Array.Empty<Guid>();

    public Guid? CanonicalDocumentId { get; set; }

    public bool IsExactMatch { get; init; }

    public string MatchReason { get; init; } = "Hash or metadata similarity";
}
