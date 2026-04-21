using PaperDesk.Domain.Entities;
using PaperDesk.Application.Queries;

namespace PaperDesk.Application.Abstractions;

public interface IDocumentIndexService
{
    Task IndexAsync(DocumentRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DocumentRecord>> SearchAsync(string query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DocumentRecord>> SearchAsync(DocumentSearchRequest request, CancellationToken cancellationToken);
}
