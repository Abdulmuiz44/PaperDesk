using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Application.Abstractions;

public interface IRenameSuggestionService
{
    Task<RenameMovePreview> BuildPreviewAsync(DocumentMetadata metadata, string? destinationDirectory, CancellationToken cancellationToken);
}
