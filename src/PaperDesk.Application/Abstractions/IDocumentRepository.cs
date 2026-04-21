using PaperDesk.Domain.Entities;

namespace PaperDesk.Application.Abstractions;

public interface IDocumentRepository
{
    Task AddAsync(DocumentRecord record, CancellationToken cancellationToken);

    Task<DocumentRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DocumentRecord>> ListAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DocumentRecord>> GetPendingAsync(CancellationToken cancellationToken);

    Task UpdateAsync(DocumentRecord record, CancellationToken cancellationToken);
}
