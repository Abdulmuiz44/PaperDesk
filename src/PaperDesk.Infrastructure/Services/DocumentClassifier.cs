using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;

namespace PaperDesk.Infrastructure.Services;

public sealed class DocumentClassifier : IDocumentClassifier
{
    public Task<DocumentType> ClassifyAsync(DocumentRecord record, CancellationToken cancellationToken)
        => Task.FromResult(DocumentType.Unknown);
}
