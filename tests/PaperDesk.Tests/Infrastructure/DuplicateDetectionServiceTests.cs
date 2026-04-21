using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;
using PaperDesk.Infrastructure.Duplicates;

namespace PaperDesk.Tests.Infrastructure;

public sealed class DuplicateDetectionServiceTests
{
    [Fact]
    public async Task FindDuplicatesAsync_ReturnsExactHashGroups()
    {
        var records = new[]
        {
            new DocumentRecord
            {
                OriginalPath = @"C:\docs\invoice-a.pdf",
                CurrentPath = @"C:\docs\invoice-a.pdf",
                Sha256Hash = "ABC123",
                DocumentType = DocumentType.Invoice,
                Status = ProcessingStatus.Completed
            },
            new DocumentRecord
            {
                OriginalPath = @"C:\docs\invoice-a-copy.pdf",
                CurrentPath = @"C:\docs\invoice-a-copy.pdf",
                Sha256Hash = "ABC123",
                DocumentType = DocumentType.Invoice,
                Status = ProcessingStatus.Completed
            },
            new DocumentRecord
            {
                OriginalPath = @"C:\docs\other.pdf",
                CurrentPath = @"C:\docs\other.pdf",
                Sha256Hash = "XYZ999",
                DocumentType = DocumentType.Statement,
                Status = ProcessingStatus.Completed
            }
        };

        var service = new DuplicateDetectionService(new InMemoryDocumentRepository(records));

        var groups = await service.FindDuplicatesAsync(CancellationToken.None);

        var exact = Assert.Single(groups.Where(group => group.IsExactMatch));
        Assert.Equal(2, exact.DocumentIds.Count);
    }

    [Fact]
    public async Task FindDuplicatesAsync_ReturnsPotentialGroupsFromExtractedFields()
    {
        using var temp = new TempDirectory();
        var pathA = Path.Combine(temp.Path, "invoice-april.pdf");
        var pathB = Path.Combine(temp.Path, "invoice-april-copy.pdf");
        await File.WriteAllTextAsync(pathA, "a");
        await File.WriteAllTextAsync(pathB, "a");

        var records = new[]
        {
            new DocumentRecord
            {
                OriginalPath = pathA,
                CurrentPath = pathA,
                ExtractedText = "Invoice No INV-2026-0042 Total 1200.00 Date 2026-04-09",
                DocumentType = DocumentType.Invoice,
                Status = ProcessingStatus.Completed
            },
            new DocumentRecord
            {
                OriginalPath = pathB,
                CurrentPath = pathB,
                ExtractedText = "Invoice #INV-2026-0042 Amount 1200.00 Date 2026-04-09",
                DocumentType = DocumentType.Invoice,
                Status = ProcessingStatus.Completed
            }
        };

        var service = new DuplicateDetectionService(new InMemoryDocumentRepository(records));

        var groups = await service.FindDuplicatesAsync(CancellationToken.None);

        var potential = Assert.Single(groups.Where(group => !group.IsExactMatch));
        Assert.Equal(2, potential.DocumentIds.Count);
    }

    private sealed class InMemoryDocumentRepository(IReadOnlyCollection<DocumentRecord> records) : IDocumentRepository
    {
        public Task AddAsync(DocumentRecord record, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<DocumentRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(records.FirstOrDefault(record => record.Id == id));

        public Task<IReadOnlyCollection<DocumentRecord>> ListAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(records);

        public Task<IReadOnlyCollection<DocumentRecord>> GetPendingAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<DocumentRecord>>([]);

        public Task UpdateAsync(DocumentRecord record, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
