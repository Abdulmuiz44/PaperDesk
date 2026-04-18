using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;
using PaperDesk.Infrastructure.Configuration;
using PaperDesk.Infrastructure.Indexing;
using PaperDesk.Infrastructure.Ocr;
using PaperDesk.Infrastructure.Persistence;

namespace PaperDesk.Tests.Infrastructure;

public sealed class SqliteIndexingTests
{
    [Fact]
    public async Task DatabaseInitializerCreatesDocumentAndFtsTables()
    {
        using var temp = new TempDirectory();
        var dbPath = Path.Combine(temp.Path, "paperdesk.db");
        var appSettings = BuildSettings(dbPath);
        var resolver = new SqlitePathResolver();
        var initializer = new DatabaseInitializer(resolver, NullLogger<DatabaseInitializer>.Instance);

        await initializer.InitializeAsync(appSettings.Database, CancellationToken.None);

        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = dbPath, Pooling = false }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type IN ('table','view') AND name IN ('documents','documents_fts');";
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        Assert.Contains("documents", names);
        Assert.Contains("documents_fts", names);
    }

    [Fact]
    public async Task IndexAndSearchReturnsMatchingDocuments()
    {
        using var temp = new TempDirectory();
        var dbPath = Path.Combine(temp.Path, "paperdesk.db");
        var settings = BuildSettings(dbPath);
        var resolver = new SqlitePathResolver();
        var initializer = new DatabaseInitializer(resolver, NullLogger<DatabaseInitializer>.Instance);
        await initializer.InitializeAsync(settings.Database, CancellationToken.None);

        var options = Options.Create(settings);
        var repository = new SqliteDocumentRepository(resolver, options);
        var indexService = new DocumentIndexService(resolver, options);

        var document = new DocumentRecord
        {
            OriginalPath = @"C:\Docs\invoice-acme.pdf",
            CurrentPath = @"C:\Docs\invoice-acme.pdf",
            DocumentType = DocumentType.Invoice,
            ExtractedText = "ACME invoice total 1250 paid",
            OcrConfidence = ConfidenceLevel.High,
            Status = ProcessingStatus.NeedsReview,
            LastProcessedUtc = DateTimeOffset.UtcNow
        };
        await repository.AddAsync(document, CancellationToken.None);
        await indexService.IndexAsync(document, CancellationToken.None);

        var results = await indexService.SearchAsync("ACME 1250", CancellationToken.None);

        Assert.Single(results);
        var first = results.First();
        Assert.Equal(document.Id, first.Id);
        Assert.Contains("ACME", first.ExtractedText ?? string.Empty);
    }

    [Fact]
    public async Task OcrServiceExtractsTextFromPlainTextFile()
    {
        using var temp = new TempDirectory();
        var filePath = Path.Combine(temp.Path, "note.txt");
        await File.WriteAllTextAsync(filePath, "Invoice alpha 2026");
        var service = new LocalOcrService();

        var result = await service.ExtractTextAsync(filePath, CancellationToken.None);

        Assert.Contains("Invoice alpha 2026", result.ExtractedText);
        Assert.Equal(ConfidenceLevel.High, result.Confidence);
    }

    private static AppSettings BuildSettings(string dbPath)
    {
        return new AppSettings
        {
            Database = new DatabaseSettings
            {
                FilePath = dbPath
            }
        };
    }
}
