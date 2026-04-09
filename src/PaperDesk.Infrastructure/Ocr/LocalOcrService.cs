using PaperDesk.Application.Abstractions;
using PaperDesk.Application.DTOs;
using PaperDesk.Domain.Enums;

namespace PaperDesk.Infrastructure.Ocr;

public sealed class LocalOcrService : IOcrService
{
    public Task<OcrResultDto> ExtractTextAsync(string filePath, CancellationToken cancellationToken)
    {
        // Placeholder: real OCR provider integration intentionally deferred.
        var result = new OcrResultDto(string.Empty, ConfidenceLevel.Low, "en-US");
        return Task.FromResult(result);
    }
}
