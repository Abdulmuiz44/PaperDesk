using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Enums;
using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Application.Services;

public sealed class DocumentIngestionCoordinator(
    IFileProcessingQueue queue,
    IDocumentMetadataExtractor metadataExtractor,
    IRenameSuggestionService renameSuggestionService,
    IActivityLog activityLog)
{
    public async Task<RenameMovePreview?> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var item = await queue.TryDequeueAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        try
        {
            var metadata = await metadataExtractor.ExtractAsync(item.FullPath, cancellationToken);
            await activityLog.WriteAsync(new ActivityEvent(
                ActivityType.FileAnalyzed,
                $"Analyzed {metadata.FileName}",
                metadata.FullPath), cancellationToken);

            var preview = await renameSuggestionService.BuildPreviewAsync(metadata, null, cancellationToken);
            await activityLog.WriteAsync(new ActivityEvent(
                ActivityType.RenameSuggested,
                $"Suggested {preview.ProposedFileName}",
                metadata.FullPath), cancellationToken);

            await queue.MarkCompletedAsync(item.FullPath, cancellationToken);
            return preview;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await queue.MarkFailedAsync(item.FullPath, ex.Message, cancellationToken);
            await activityLog.WriteAsync(new ActivityEvent(
                ActivityType.Failure,
                ex.Message,
                item.FullPath), cancellationToken);
            return null;
        }
    }
}
