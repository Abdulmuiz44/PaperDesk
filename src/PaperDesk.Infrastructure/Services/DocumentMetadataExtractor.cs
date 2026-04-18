using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Infrastructure.Services;

public sealed class DocumentMetadataExtractor : IDocumentMetadataExtractor
{
    public async Task<DocumentMetadata> ExtractAsync(string fullPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Document file was not found.", fullPath);
        }

        await WaitUntilReadableAsync(fullPath, cancellationToken);

        var info = new FileInfo(fullPath);
        return new DocumentMetadata(
            info.FullName,
            info.Name,
            info.Extension.ToLowerInvariant(),
            info.Length,
            info.CreationTimeUtc,
            info.LastWriteTimeUtc);
    }

    private static async Task WaitUntilReadableAsync(string fullPath, CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;
        var delay = TimeSpan.FromMilliseconds(150);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
                delay += delay;
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
                delay += delay;
            }
        }

        await using var finalStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }
}
