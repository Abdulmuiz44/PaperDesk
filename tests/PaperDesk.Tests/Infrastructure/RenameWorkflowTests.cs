using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;
using PaperDesk.Infrastructure.Configuration;
using PaperDesk.Infrastructure.Persistence;
using PaperDesk.Infrastructure.Services;

namespace PaperDesk.Tests.Infrastructure;

public sealed class RenameWorkflowTests
{
    [Fact]
    public async Task DocumentClassifierInfersInvoiceType()
    {
        var classifier = new DocumentClassifier();
        var record = new DocumentRecord
        {
            OriginalPath = @"C:\Inbox\scan001.pdf",
            ExtractedText = "Invoice No 1234\nBill To ACME",
            OcrConfidence = ConfidenceLevel.High
        };

        var type = await classifier.ClassifyAsync(record, CancellationToken.None);

        Assert.Equal(DocumentType.Invoice, type);
    }

    [Fact]
    public async Task RenamingServiceCreatesTemplateBasedFilenameAndRoute()
    {
        var service = new RenamingService();
        var record = new DocumentRecord
        {
            OriginalPath = @"C:\Inbox\receipt-a.pdf",
            CurrentPath = @"C:\Inbox\receipt-a.pdf",
            DocumentType = DocumentType.Receipt,
            ExtractedText = "Store XYZ\nTotal 42.50",
            OcrConfidence = ConfidenceLevel.Medium,
            DiscoveredUtc = new DateTimeOffset(2026, 4, 19, 0, 0, 0, TimeSpan.Zero),
            LastProcessedUtc = new DateTimeOffset(2026, 4, 19, 0, 0, 0, TimeSpan.Zero)
        };

        var plan = await service.BuildSuggestionAsync(record, CancellationToken.None);

        Assert.Contains("2026-04-19_receipt", plan.ProposedFileName.ToLowerInvariant());
        Assert.Contains(Path.Combine("Sorted", "Receipts", "2026"), plan.ProposedDirectory ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RenameSuggestionRepositoryPersistsAndLoadsPendingItems()
    {
        using var temp = new TempDirectory();
        var dbPath = Path.Combine(temp.Path, "paperdesk.db");
        var settings = new AppSettings { Database = new DatabaseSettings { FilePath = dbPath } };
        var options = Options.Create(settings);
        var resolver = new SqlitePathResolver();
        var initializer = new DatabaseInitializer(resolver, NullLogger<DatabaseInitializer>.Instance);
        await initializer.InitializeAsync(settings.Database, CancellationToken.None);

        var docRepo = new SqliteDocumentRepository(resolver, options);
        var suggestionRepo = new SqliteRenameSuggestionRepository(resolver, options);

        var record = new DocumentRecord
        {
            OriginalPath = @"C:\Inbox\doc.pdf",
            CurrentPath = @"C:\Inbox\doc.pdf",
            DocumentType = DocumentType.Other,
            Status = ProcessingStatus.NeedsReview
        };
        await docRepo.AddAsync(record, CancellationToken.None);

        var suggestion = new RenameSuggestion
        {
            DocumentId = record.Id,
            ProposedFileName = "2026-04-19_other_doc_na.pdf",
            ProposedDestinationDirectory = @"C:\Inbox\Sorted\Other\2026",
            Confidence = ConfidenceLevel.Medium,
            Reason = "rule test"
        };
        await suggestionRepo.AddAsync(suggestion, CancellationToken.None);

        var pending = await suggestionRepo.ListForReviewQueueAsync(CancellationToken.None);
        Assert.Single(pending);

        var approved = pending.First();
        await suggestionRepo.UpdateAsync(new RenameSuggestion
        {
            Id = approved.Id,
            DocumentId = approved.DocumentId,
            ProposedFileName = approved.ProposedFileName,
            ProposedDestinationDirectory = approved.ProposedDestinationDirectory,
            Confidence = approved.Confidence,
            Reason = approved.Reason,
            IsApproved = true,
            IsSkipped = false
        }, CancellationToken.None);

        var remaining = await suggestionRepo.ListForReviewQueueAsync(CancellationToken.None);
        Assert.Empty(remaining);
    }
}
