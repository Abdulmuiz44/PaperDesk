using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;

namespace PaperDesk.Application.Abstractions;

public interface IDocumentClassifier
{
    Task<DocumentType> ClassifyAsync(DocumentRecord record, CancellationToken cancellationToken);
}
