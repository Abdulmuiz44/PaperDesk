using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Infrastructure.Indexing;

public sealed class DocumentIndexService : IDocumentIndexService
{
    public Task IndexAsync(DocumentRecord record, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<IReadOnlyCollection<DocumentRecord>> SearchAsync(string query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<DocumentRecord>>(Array.Empty<DocumentRecord>());
}
