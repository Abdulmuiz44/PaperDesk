using PaperDesk.Domain.Enums;

namespace PaperDesk.Application.DTOs;

public sealed record OcrResultDto(
    string ExtractedText,
    ConfidenceLevel Confidence,
    string? Language);
