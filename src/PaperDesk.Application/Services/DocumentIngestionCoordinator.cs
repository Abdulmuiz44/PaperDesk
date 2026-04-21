using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;
using PaperDesk.Domain.ValueObjects;
using System.Security.Cryptography;

namespace PaperDesk.Application.Services;

public sealed class DocumentIngestionCoordinator(
    IFileProcessingQueue queue,
    IDocumentMetadataExtractor metadataExtractor,
    IRenameSuggestionService renameSuggestionService,
    IRenamingService renamingService,
    IDocumentClassifier documentClassifier,
    IOcrService ocrService,
    IDocumentRepository documentRepository,
    IDocumentIndexService documentIndexService,
    IRenameSuggestionRepository renameSuggestionRepository,
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

            var ocrResult = await ocrService.ExtractTextAsync(metadata.FullPath, cancellationToken);
            await activityLog.WriteAsync(new ActivityEvent(
                ActivityType.OcrCompleted,
                $"OCR completed for {metadata.FileName}",
                metadata.FullPath), cancellationToken);

            var documentRecord = new DocumentRecord
            {
                OriginalPath = metadata.FullPath,
                CurrentPath = metadata.FullPath,
                DocumentType = DocumentType.Unknown,
                ExtractedText = ocrResult.ExtractedText,
                OcrConfidence = ocrResult.Confidence,
                Sha256Hash = await ComputeSha256Async(metadata.FullPath, cancellationToken),
                Status = ProcessingStatus.NeedsReview,
                LastProcessedUtc = DateTimeOffset.UtcNow
            };

            documentRecord.DocumentType = await documentClassifier.ClassifyAsync(documentRecord, cancellationToken);

            await documentRepository.AddAsync(documentRecord, cancellationToken);
            await documentIndexService.IndexAsync(documentRecord, cancellationToken);

            var preview = await renameSuggestionService.BuildPreviewAsync(metadata, null, cancellationToken);
            var plan = await renamingService.BuildSuggestionAsync(documentRecord, cancellationToken);
            var suggestion = new RenameSuggestion
            {
                DocumentId = documentRecord.Id,
                ProposedFileName = plan.ProposedFileName,
                ProposedDestinationDirectory = plan.ProposedDirectory,
                Confidence = plan.Confidence,
                Reason = plan.Reason
            };
            await renameSuggestionRepository.AddAsync(suggestion, cancellationToken);
            await activityLog.WriteAsync(new ActivityEvent(
                ActivityType.RenameSuggested,
                $"Suggested {preview.ProposedFileName}",
                metadata.FullPath), cancellationToken);
            await activityLog.WriteAsync(new ActivityEvent(
                ActivityType.MoveSuggested,
                $"Destination {plan.ProposedDirectory}",
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

    private static async Task<string?> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
