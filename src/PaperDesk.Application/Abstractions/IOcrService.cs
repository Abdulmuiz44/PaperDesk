using PaperDesk.Application.DTOs;

namespace PaperDesk.Application.Abstractions;

public interface IOcrService
{
    Task<OcrResultDto> ExtractTextAsync(string filePath, CancellationToken cancellationToken);
}
