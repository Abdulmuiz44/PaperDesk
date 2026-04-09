using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Infrastructure.Persistence;

public sealed class SqliteDocumentRepository : IDocumentRepository
{
    public Task AddAsync(DocumentRecord record, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<DocumentRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult<DocumentRecord?>(null);

    public Task<IReadOnlyCollection<DocumentRecord>> GetPendingAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<DocumentRecord>>(Array.Empty<DocumentRecord>());

    public Task UpdateAsync(DocumentRecord record, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
