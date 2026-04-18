using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Application.Abstractions;

public interface IDocumentMetadataExtractor
{
    Task<DocumentMetadata> ExtractAsync(string fullPath, CancellationToken cancellationToken);
}
