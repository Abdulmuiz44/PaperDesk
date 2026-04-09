namespace PaperDesk.Domain.Entities;

public sealed class WatchedFolder
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Path { get; init; }

    public bool IsRecursive { get; init; } = true;

    public bool IsEnabled { get; set; } = true;

    public IReadOnlyCollection<string> IncludedExtensions { get; init; } = new[] { ".pdf", ".jpg", ".png" };

    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
