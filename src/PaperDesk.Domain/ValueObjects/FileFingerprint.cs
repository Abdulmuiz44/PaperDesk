namespace PaperDesk.Domain.ValueObjects;

public readonly record struct FileFingerprint(string Sha256Hash, long SizeBytes);
