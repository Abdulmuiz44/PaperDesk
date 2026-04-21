using PaperDesk.Application.Abstractions;
using PaperDesk.Application.DTOs;
using PaperDesk.Domain.Enums;
using Tesseract;
using UglyToad.PdfPig;

namespace PaperDesk.Infrastructure.Ocr;

public sealed class LocalOcrService : IOcrService
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".csv", ".json", ".xml", ".md", ".log"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif"
    };

    public async Task<OcrResultDto> ExtractTextAsync(string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found for OCR.", filePath);
        }

        var extension = Path.GetExtension(filePath);
        if (TextExtensions.Contains(extension))
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            return new OcrResultDto(content, string.IsNullOrWhiteSpace(content) ? ConfidenceLevel.Low : ConfidenceLevel.High, "text/plain");
        }

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return await ExtractPdfTextAsync(filePath, cancellationToken);
        }

        if (ImageExtensions.Contains(extension))
        {
            return await ExtractImageTextAsync(filePath, cancellationToken);
        }

        return new OcrResultDto(string.Empty, ConfidenceLevel.Low, null);
    }

    private static Task<OcrResultDto> ExtractPdfTextAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var document = PdfDocument.Open(filePath);
        var pageTexts = document.GetPages()
            .Select(page => page.Text?.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        var extracted = string.Join(Environment.NewLine + Environment.NewLine, pageTexts);
        if (!string.IsNullOrWhiteSpace(extracted))
        {
            return Task.FromResult(new OcrResultDto(extracted, ConfidenceLevel.High, "pdf-text"));
        }

        return Task.FromResult(new OcrResultDto(string.Empty, ConfidenceLevel.Low, "pdf-text"));
    }

    private static async Task<OcrResultDto> ExtractImageTextAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tessDataPath = await EnsureTessDataAsync(cancellationToken);
        using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
        using var image = Pix.LoadFromFile(filePath);
        using var page = engine.Process(image);
        var text = page.GetText() ?? string.Empty;
        var confidence = MapConfidence(page.GetMeanConfidence());

        return new OcrResultDto(text.Trim(), confidence, "eng");
    }

    private static ConfidenceLevel MapConfidence(float meanConfidence)
    {
        if (meanConfidence >= 0.80f)
        {
            return ConfidenceLevel.High;
        }

        if (meanConfidence >= 0.45f)
        {
            return ConfidenceLevel.Medium;
        }

        return ConfidenceLevel.Low;
    }

    private static async Task<string> EnsureTessDataAsync(CancellationToken cancellationToken)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var tessDataPath = Path.Combine(appData, "PaperDesk", "tessdata");
        Directory.CreateDirectory(tessDataPath);

        var languageFilePath = Path.Combine(tessDataPath, "eng.traineddata");
        if (File.Exists(languageFilePath))
        {
            return tessDataPath;
        }

        using var client = new HttpClient();
        using var response = await client.GetAsync(
            "https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(languageFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await input.CopyToAsync(output, cancellationToken);

        return tessDataPath;
    }
}
