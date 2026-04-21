using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;

namespace PaperDesk.Infrastructure.Services;

public sealed class DocumentClassifier : IDocumentClassifier
{
    public Task<DocumentType> ClassifyAsync(DocumentRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var haystack = $"{record.OriginalPath} {record.ExtractedText}".ToLowerInvariant();

        if (ContainsAny(haystack, "invoice", "vat", "bill to", "invoice no", "inv #"))
        {
            return Task.FromResult(DocumentType.Invoice);
        }

        if (ContainsAny(haystack, "receipt", "paid", "point of sale", "thank you for your purchase"))
        {
            return Task.FromResult(DocumentType.Receipt);
        }

        if (ContainsAny(haystack, "statement", "account balance", "bank statement", "opening balance", "closing balance"))
        {
            return Task.FromResult(DocumentType.Statement);
        }

        if (ContainsAny(haystack, "agreement", "contract", "terms and conditions", "signature"))
        {
            return Task.FromResult(DocumentType.Contract);
        }

        if (ContainsAny(haystack, "passport", "national id", "driver license", "date of birth"))
        {
            return Task.FromResult(DocumentType.Identity);
        }

        return Task.FromResult(DocumentType.Other);
    }

    private static bool ContainsAny(string value, params string[] terms)
        => terms.Any(value.Contains);
}
