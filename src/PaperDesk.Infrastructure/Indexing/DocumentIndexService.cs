using PaperDesk.Application.Abstractions;
using PaperDesk.Application.Queries;
using PaperDesk.Domain.Entities;
using PaperDesk.Infrastructure.Configuration;
using PaperDesk.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace PaperDesk.Infrastructure.Indexing;

public sealed class DocumentIndexService(
    SqlitePathResolver pathResolver,
    IOptions<AppSettings> appSettings) : IDocumentIndexService
{
    public async Task IndexAsync(DocumentRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using (var deleteFtsCommand = connection.CreateCommand())
        {
            deleteFtsCommand.CommandText = "DELETE FROM documents_fts WHERE id = $id;";
            deleteFtsCommand.Parameters.AddWithValue("$id", record.Id.ToString("D"));
            await deleteFtsCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insertFtsCommand = connection.CreateCommand())
        {
            insertFtsCommand.CommandText = """
                INSERT INTO documents_fts(id, content_text, original_path, current_path)
                VALUES ($id, $content_text, $original_path, $current_path);
                """;
            insertFtsCommand.Parameters.AddWithValue("$id", record.Id.ToString("D"));
            insertFtsCommand.Parameters.AddWithValue("$content_text", record.ExtractedText ?? string.Empty);
            insertFtsCommand.Parameters.AddWithValue("$original_path", record.OriginalPath);
            insertFtsCommand.Parameters.AddWithValue("$current_path", (object?)record.CurrentPath ?? DBNull.Value);
            await insertFtsCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<DocumentRecord>> SearchAsync(string query, CancellationToken cancellationToken)
        => await SearchAsync(new DocumentSearchRequest(query), cancellationToken);

    public async Task<IReadOnlyCollection<DocumentRecord>> SearchAsync(DocumentSearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.QueryText))
        {
            return Array.Empty<DocumentRecord>();
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        var sql = """
            SELECT
                d.id,
                d.original_path,
                d.current_path,
                d.document_type,
                d.extracted_text,
                d.ocr_confidence,
                d.sha256_hash,
                d.status,
                d.discovered_utc,
                d.last_processed_utc
            FROM documents_fts f
            JOIN documents d ON d.id = f.id
            WHERE documents_fts MATCH $query
            """;
        if (request.DocumentType.HasValue)
        {
            sql += " AND d.document_type = $document_type";
            command.Parameters.AddWithValue("$document_type", (int)request.DocumentType.Value);
        }

        if (request.Status.HasValue)
        {
            sql += " AND d.status = $status";
            command.Parameters.AddWithValue("$status", (int)request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SourceFolder))
        {
            sql += " AND (d.current_path LIKE $source_prefix OR d.original_path LIKE $source_prefix)";
            command.Parameters.AddWithValue("$source_prefix", $"{request.SourceFolder.Trim()}%");
        }

        if (request.FromDateUtc.HasValue)
        {
            sql += " AND d.discovered_utc >= $from_date";
            command.Parameters.AddWithValue("$from_date", request.FromDateUtc.Value.ToString("O"));
        }

        if (request.ToDateUtc.HasValue)
        {
            sql += " AND d.discovered_utc <= $to_date";
            command.Parameters.AddWithValue("$to_date", request.ToDateUtc.Value.ToString("O"));
        }

        sql += " ORDER BY bm25(documents_fts), d.discovered_utc DESC LIMIT 100;";
        command.CommandText = sql;
        command.Parameters.AddWithValue("$query", BuildMatchExpression(request.QueryText));

        var results = new List<DocumentRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DocumentRecord
            {
                Id = Guid.Parse(reader.GetString(0)),
                OriginalPath = reader.GetString(1),
                CurrentPath = reader.IsDBNull(2) ? null : reader.GetString(2),
                DocumentType = (PaperDesk.Domain.Enums.DocumentType)reader.GetInt32(3),
                ExtractedText = reader.IsDBNull(4) ? null : reader.GetString(4),
                OcrConfidence = (PaperDesk.Domain.Enums.ConfidenceLevel)reader.GetInt32(5),
                Sha256Hash = reader.IsDBNull(6) ? null : reader.GetString(6),
                Status = (PaperDesk.Domain.Enums.ProcessingStatus)reader.GetInt32(7),
                DiscoveredUtc = DateTimeOffset.Parse(reader.GetString(8)),
                LastProcessedUtc = reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9))
            });
        }

        return results;
    }

    private SqliteConnection CreateConnection()
    {
        var dbPath = pathResolver.ResolvePath(appSettings.Value.Database);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Pooling = false
        }.ToString();
        return new SqliteConnection(connectionString);
    }

    private static string BuildMatchExpression(string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0)
        {
            return "\"\"";
        }

        return string.Join(" AND ", terms.Select(term => $"\"{term.Replace("\"", "\"\"")}\""));
    }
}
