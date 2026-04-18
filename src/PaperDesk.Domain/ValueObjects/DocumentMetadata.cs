namespace PaperDesk.Domain.ValueObjects;

public sealed record DocumentMetadata(
    string FullPath,
    string FileName,
    string Extension,
    long SizeBytes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ModifiedUtc);
