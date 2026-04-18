using PaperDesk.Application.Abstractions;
using PaperDesk.Application.DTOs;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;
using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.Infrastructure.Services;

public sealed class RenamingService : IRenamingService, IRenameSuggestionService
{
    public Task<RenamePlanDto> BuildSuggestionAsync(DocumentRecord record, CancellationToken cancellationToken)
    {
        var metadata = new DocumentMetadata(
            record.OriginalPath,
            Path.GetFileName(record.OriginalPath),
            Path.GetExtension(record.OriginalPath).ToLowerInvariant(),
            0,
            record.DiscoveredUtc,
            record.DiscoveredUtc);

        return BuildPreviewAsync(metadata, Path.GetDirectoryName(record.OriginalPath), cancellationToken)
            .ContinueWith(task => new RenamePlanDto(
                record.Id,
                task.Result.ProposedFileName,
                task.Result.ProposedDirectory,
                task.Result.Confidence,
                task.Result.Reason), cancellationToken);
    }

    public Task<RenameMovePreview> BuildPreviewAsync(DocumentMetadata metadata, string? destinationDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = string.IsNullOrWhiteSpace(destinationDirectory)
            ? Path.GetDirectoryName(metadata.FullPath) ?? Environment.CurrentDirectory
            : Path.GetFullPath(destinationDirectory);

        var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(metadata.FileName));
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "Document";
        }

        var datedBaseName = $"{baseName} - {metadata.ModifiedUtc:yyyy-MM-dd}";
        var proposedFileName = EnsureUniqueFileName(directory, datedBaseName, metadata.Extension, metadata.FullPath);
        var proposedPath = Path.Combine(directory, proposedFileName);

        var preview = new RenameMovePreview(
            metadata.FullPath,
            proposedPath,
            proposedFileName,
            directory,
            ConfidenceLevel.Low,
            "Phase 1 filename/date preview; no file operation will be applied.");

        return Task.FromResult(preview);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.Join(' ', sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string EnsureUniqueFileName(string directory, string baseName, string extension, string originalPath)
    {
        var suffix = 0;
        while (true)
        {
            var candidateName = suffix == 0
                ? $"{baseName}{extension}"
                : $"{baseName} ({suffix + 1}){extension}";
            var candidatePath = Path.Combine(directory, candidateName);

            if (!File.Exists(candidatePath) || string.Equals(Path.GetFullPath(candidatePath), Path.GetFullPath(originalPath), StringComparison.OrdinalIgnoreCase))
            {
                return candidateName;
            }

            suffix++;
        }
    }
}
