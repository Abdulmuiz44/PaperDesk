using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;
using PaperDesk.Infrastructure.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace PaperDesk.Infrastructure.Persistence;

public sealed class SqliteDocumentRepository(
    SqlitePathResolver pathResolver,
    IOptions<AppSettings> appSettings) : IDocumentRepository
{
    public async Task AddAsync(DocumentRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO documents (
                id,
                original_path,
                current_path,
                document_type,
                extracted_text,
                ocr_confidence,
                sha256_hash,
                status,
                discovered_utc,
                last_processed_utc
            ) VALUES (
                $id,
                $original_path,
                $current_path,
                $document_type,
                $extracted_text,
                $ocr_confidence,
                $sha256_hash,
                $status,
                $discovered_utc,
                $last_processed_utc
            )
            ON CONFLICT(id) DO UPDATE SET
                original_path = excluded.original_path,
                current_path = excluded.current_path,
                document_type = excluded.document_type,
                extracted_text = excluded.extracted_text,
                ocr_confidence = excluded.ocr_confidence,
                sha256_hash = excluded.sha256_hash,
                status = excluded.status,
                discovered_utc = excluded.discovered_utc,
                last_processed_utc = excluded.last_processed_utc;
            """;
        BindRecord(command, record);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DocumentRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                id,
                original_path,
                current_path,
                document_type,
                extracted_text,
                ocr_confidence,
                sha256_hash,
                status,
                discovered_utc,
                last_processed_utc
            FROM documents
            WHERE id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadRecord(reader);
    }

    public async Task<IReadOnlyCollection<DocumentRecord>> GetPendingAsync(CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                id,
                original_path,
                current_path,
                document_type,
                extracted_text,
                ocr_confidence,
                sha256_hash,
                status,
                discovered_utc,
                last_processed_utc
            FROM documents
            WHERE status = $pending OR status = $queued OR status = $processing
            ORDER BY discovered_utc ASC;
            """;
        command.Parameters.AddWithValue("$pending", (int)PaperDesk.Domain.Enums.ProcessingStatus.Pending);
        command.Parameters.AddWithValue("$queued", (int)PaperDesk.Domain.Enums.ProcessingStatus.Queued);
        command.Parameters.AddWithValue("$processing", (int)PaperDesk.Domain.Enums.ProcessingStatus.Processing);

        var results = new List<DocumentRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ReadRecord(reader));
        }

        return results;
    }

    public Task UpdateAsync(DocumentRecord record, CancellationToken cancellationToken)
        => AddAsync(record, cancellationToken);

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

    private static void BindRecord(SqliteCommand command, DocumentRecord record)
    {
        command.Parameters.AddWithValue("$id", record.Id.ToString("D"));
        command.Parameters.AddWithValue("$original_path", record.OriginalPath);
        command.Parameters.AddWithValue("$current_path", (object?)record.CurrentPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$document_type", (int)record.DocumentType);
        command.Parameters.AddWithValue("$extracted_text", (object?)record.ExtractedText ?? DBNull.Value);
        command.Parameters.AddWithValue("$ocr_confidence", (int)record.OcrConfidence);
        command.Parameters.AddWithValue("$sha256_hash", (object?)record.Sha256Hash ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", (int)record.Status);
        command.Parameters.AddWithValue("$discovered_utc", record.DiscoveredUtc.ToString("O"));
        command.Parameters.AddWithValue("$last_processed_utc", record.LastProcessedUtc?.ToString("O") ?? (object)DBNull.Value);
    }

    private static DocumentRecord ReadRecord(SqliteDataReader reader)
    {
        return new DocumentRecord
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
        };
    }
}
