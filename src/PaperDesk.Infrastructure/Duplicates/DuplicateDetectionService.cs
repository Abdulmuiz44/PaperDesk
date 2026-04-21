using System.Text.RegularExpressions;
using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Infrastructure.Duplicates;

public sealed partial class DuplicateDetectionService(IDocumentRepository documentRepository) : IDuplicateDetectionService
{
    public async Task<IReadOnlyCollection<DuplicateGroup>> FindDuplicatesAsync(CancellationToken cancellationToken)
    {
        var records = await documentRepository.ListAllAsync(cancellationToken);
        if (records.Count < 2)
        {
            return Array.Empty<DuplicateGroup>();
        }

        var groups = new List<DuplicateGroup>();

        var exactHashGroups = records
            .Where(record => !string.IsNullOrWhiteSpace(record.Sha256Hash))
            .GroupBy(record => record.Sha256Hash!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToArray();

        foreach (var hashGroup in exactHashGroups)
        {
            var members = hashGroup
                .Select(record => record.Id)
                .Distinct()
                .ToArray();
            groups.Add(new DuplicateGroup
            {
                DocumentIds = members,
                CanonicalDocumentId = members[0],
                IsExactMatch = true,
                MatchReason = "Exact content hash match (SHA-256)"
            });
        }

        var exactMembers = new HashSet<Guid>(groups.SelectMany(group => group.DocumentIds));
        var potentialMembers = records.Where(record => !exactMembers.Contains(record.Id)).ToArray();
        if (potentialMembers.Length < 2)
        {
            return groups;
        }

        var clusters = BuildPotentialClusters(potentialMembers);
        foreach (var cluster in clusters)
        {
            var members = cluster.Select(record => record.Id).Distinct().ToArray();
            groups.Add(new DuplicateGroup
            {
                DocumentIds = members,
                CanonicalDocumentId = members[0],
                IsExactMatch = false,
                MatchReason = "Potential duplicate (name, size, and extracted-field similarity)"
            });
        }

        return groups;
    }

    private static IReadOnlyCollection<IReadOnlyCollection<DocumentRecord>> BuildPotentialClusters(IReadOnlyList<DocumentRecord> records)
    {
        var parent = records.Select((_, index) => index).ToArray();

        for (var i = 0; i < records.Count - 1; i++)
        {
            for (var j = i + 1; j < records.Count; j++)
            {
                if (!IsPotentialMatch(records[i], records[j]))
                {
                    continue;
                }

                Union(parent, i, j);
            }
        }

        var grouped = new Dictionary<int, List<DocumentRecord>>();
        for (var index = 0; index < records.Count; index++)
        {
            var root = Find(parent, index);
            if (!grouped.TryGetValue(root, out var bucket))
            {
                bucket = [];
                grouped[root] = bucket;
            }

            bucket.Add(records[index]);
        }

        return grouped.Values.Where(bucket => bucket.Count > 1).ToArray();
    }

    private static bool IsPotentialMatch(DocumentRecord left, DocumentRecord right)
    {
        var leftName = NormalizeName(Path.GetFileNameWithoutExtension(left.CurrentPath ?? left.OriginalPath));
        var rightName = NormalizeName(Path.GetFileNameWithoutExtension(right.CurrentPath ?? right.OriginalPath));

        var leftPath = left.CurrentPath ?? left.OriginalPath;
        var rightPath = right.CurrentPath ?? right.OriginalPath;
        var leftSize = TryGetFileSize(leftPath);
        var rightSize = TryGetFileSize(rightPath);

        var similarSize = leftSize > 0
            && rightSize > 0
            && Math.Abs(leftSize.Value - rightSize.Value) <= Math.Max(leftSize.Value, rightSize.Value) * 0.02;
        var sameName = !string.IsNullOrWhiteSpace(leftName) && string.Equals(leftName, rightName, StringComparison.Ordinal);

        if (sameName && similarSize)
        {
            return true;
        }

        var leftFields = ExtractMatchFields(left.ExtractedText);
        var rightFields = ExtractMatchFields(right.ExtractedText);

        var matchingInvoice = !string.IsNullOrWhiteSpace(leftFields.InvoiceNumber)
            && string.Equals(leftFields.InvoiceNumber, rightFields.InvoiceNumber, StringComparison.OrdinalIgnoreCase);
        var matchingAmount = !string.IsNullOrWhiteSpace(leftFields.Amount)
            && string.Equals(leftFields.Amount, rightFields.Amount, StringComparison.OrdinalIgnoreCase);
        var matchingDate = !string.IsNullOrWhiteSpace(leftFields.Date)
            && string.Equals(leftFields.Date, rightFields.Date, StringComparison.OrdinalIgnoreCase);

        return (matchingInvoice && matchingAmount)
            || (sameName && matchingAmount)
            || (matchingAmount && matchingDate && similarSize);
    }

    private static long? TryGetFileSize(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            return new FileInfo(path).Length;
        }
        catch
        {
            return null;
        }
    }

    private static int Find(int[] parent, int index)
    {
        while (parent[index] != index)
        {
            parent[index] = parent[parent[index]];
            index = parent[index];
        }

        return index;
    }

    private static void Union(int[] parent, int first, int second)
    {
        var firstRoot = Find(parent, first);
        var secondRoot = Find(parent, second);
        if (firstRoot != secondRoot)
        {
            parent[secondRoot] = firstRoot;
        }
    }

    private static string NormalizeName(string value)
        => new(value
            .Where(character => char.IsLetterOrDigit(character))
            .Select(char.ToLowerInvariant)
            .ToArray());

    private static DuplicateMatchFields ExtractMatchFields(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new DuplicateMatchFields(null, null, null);
        }

        var invoiceNumber = InvoiceNumberRegex().Match(text).Groups[1].Value;
        var amount = AmountRegex().Match(text).Groups[1].Value;
        var date = DateRegex().Match(text).Groups[1].Value;

        return new DuplicateMatchFields(
            string.IsNullOrWhiteSpace(invoiceNumber) ? null : invoiceNumber.Trim(),
            string.IsNullOrWhiteSpace(amount) ? null : amount.Trim(),
            string.IsNullOrWhiteSpace(date) ? null : date.Trim());
    }

    [GeneratedRegex(@"(?:invoice|inv)\s*(?:number|no|#)?\s*[:\-]?\s*([A-Z0-9\-]{3,})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceNumberRegex();

    [GeneratedRegex(@"(?:total|amount|balance)\s*[:\-]?\s*([$€£]?[0-9]+(?:[.,][0-9]{2})?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"\b((?:20\d{2}[-/]\d{2}[-/]\d{2})|(?:\d{2}[-/]\d{2}[-/](?:20)?\d{2}))\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DateRegex();

    private sealed record DuplicateMatchFields(string? InvoiceNumber, string? Amount, string? Date);
}
