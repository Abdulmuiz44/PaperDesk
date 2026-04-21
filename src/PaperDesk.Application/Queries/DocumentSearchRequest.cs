using PaperDesk.Domain.Enums;

namespace PaperDesk.Application.Queries;

public sealed record DocumentSearchRequest(
    string QueryText,
    DocumentType? DocumentType = null,
    ProcessingStatus? Status = null,
    string? SourceFolder = null,
    DateTimeOffset? FromDateUtc = null,
    DateTimeOffset? ToDateUtc = null);
