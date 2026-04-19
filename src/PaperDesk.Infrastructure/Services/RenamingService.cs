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
        cancellationToken.ThrowIfCancellationRequested();

        var originalPath = Path.GetFullPath(record.CurrentPath ?? record.OriginalPath);
        var extension = Path.GetExtension(originalPath).ToLowerInvariant();
        var sourceDirectory = Path.GetDirectoryName(originalPath) ?? Environment.CurrentDirectory;
        var baseName = BuildBaseName(record);
        var destination = BuildDestinationDirectory(sourceDirectory, record.DocumentType, record.DiscoveredUtc);
        var proposedFileName = EnsureUniqueFileName(destination, baseName, extension, originalPath);

        return Task.FromResult(new RenamePlanDto(
            record.Id,
            proposedFileName,
            destination,
            DetermineConfidence(record),
            "Template: {date}_{type}_{party}_{amount}; routing by document type."));
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

        var datedBaseName = $"{metadata.ModifiedUtc:yyyy-MM-dd}_other_{baseName}";
        var proposedFileName = EnsureUniqueFileName(directory, datedBaseName, metadata.Extension, metadata.FullPath);
        var proposedPath = Path.Combine(directory, proposedFileName);

        var preview = new RenameMovePreview(
            metadata.FullPath,
            proposedPath,
            proposedFileName,
            directory,
            ConfidenceLevel.Medium,
            "Template-based suggestion preview.");

        return Task.FromResult(preview);
    }

    private static string BuildBaseName(DocumentRecord record)
    {
        var dateToken = (record.LastProcessedUtc ?? record.DiscoveredUtc).ToString("yyyy-MM-dd");
        var typeToken = record.DocumentType.ToString().ToLowerInvariant();
        var partyToken = ExtractPartyToken(record.ExtractedText) ?? Path.GetFileNameWithoutExtension(record.OriginalPath);
        var amountToken = ExtractAmountToken(record.ExtractedText) ?? "na";

        return SanitizeFileName($"{dateToken}_{typeToken}_{partyToken}_{amountToken}");
    }

    private static string BuildDestinationDirectory(string sourceDirectory, DocumentType type, DateTimeOffset discoveredUtc)
    {
        var year = discoveredUtc.ToString("yyyy");
        var typeFolder = type switch
        {
            DocumentType.Invoice => "Invoices",
            DocumentType.Receipt => "Receipts",
            DocumentType.Statement => "Statements",
            DocumentType.Contract => "Contracts",
            DocumentType.Identity => "Identity",
            _ => "Other"
        };

        return Path.Combine(sourceDirectory, "Sorted", typeFolder, year);
    }

    private static string? ExtractPartyToken(string? extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return null;
        }

        var firstLine = extractedText
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return null;
        }

        return firstLine.Length > 32 ? firstLine[..32] : firstLine;
    }

    private static string? ExtractAmountToken(string? extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(
            extractedText,
            @"(?:\$|USD|NGN|EUR|GBP)?\s?\d{1,3}(?:[,\s]\d{3})*(?:\.\d{2})?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return null;
        }

        var cleaned = match.Value.Replace(" ", string.Empty).Replace(",", string.Empty);
        return cleaned;
    }

    private static ConfidenceLevel DetermineConfidence(DocumentRecord record)
        => record.OcrConfidence switch
        {
            ConfidenceLevel.High => ConfidenceLevel.High,
            ConfidenceLevel.Medium => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };

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
