using PaperDesk.Domain.Entities;

namespace PaperDesk.Application.Abstractions;

public interface IDocumentIndexService
{
    Task IndexAsync(DocumentRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DocumentRecord>> SearchAsync(string query, CancellationToken cancellationToken);
}
