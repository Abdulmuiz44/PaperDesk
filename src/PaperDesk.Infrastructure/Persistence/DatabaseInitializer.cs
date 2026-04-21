using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PaperDesk.Infrastructure.Configuration;

namespace PaperDesk.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly SqlitePathResolver _pathResolver;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(SqlitePathResolver pathResolver, ILogger<DatabaseInitializer> logger)
    {
        _pathResolver = pathResolver;
        _logger = logger;
    }

    public async Task InitializeAsync(DatabaseSettings settings, CancellationToken cancellationToken)
    {
        var dbPath = _pathResolver.ResolvePath(settings);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Pooling = false
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;

            CREATE TABLE IF NOT EXISTS documents (
                id TEXT PRIMARY KEY,
                original_path TEXT NOT NULL,
                current_path TEXT NULL,
                document_type INTEGER NOT NULL,
                extracted_text TEXT NULL,
                ocr_confidence INTEGER NOT NULL,
                sha256_hash TEXT NULL,
                status INTEGER NOT NULL,
                discovered_utc TEXT NOT NULL,
                last_processed_utc TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_documents_status ON documents(status);
            CREATE INDEX IF NOT EXISTS ix_documents_original_path ON documents(original_path);

            CREATE VIRTUAL TABLE IF NOT EXISTS documents_fts USING fts5(
                id UNINDEXED,
                content_text,
                original_path,
                current_path
            );

            CREATE TABLE IF NOT EXISTS rename_suggestions (
                id TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                proposed_file_name TEXT NOT NULL,
                proposed_destination_directory TEXT NULL,
                confidence INTEGER NOT NULL,
                reason TEXT NOT NULL,
                is_approved INTEGER NOT NULL DEFAULT 0,
                is_skipped INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY(document_id) REFERENCES documents(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_rename_suggestions_document_id ON rename_suggestions(document_id);
            CREATE INDEX IF NOT EXISTS ix_rename_suggestions_approved ON rename_suggestions(is_approved);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Backward-compatible migration for older local DBs that predate skip state.
        try
        {
            await using var migration = connection.CreateCommand();
            migration.CommandText = "ALTER TABLE rename_suggestions ADD COLUMN is_skipped INTEGER NOT NULL DEFAULT 0;";
            await migration.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
        {
            // Column already exists; no action needed.
        }

        _logger.LogInformation("SQLite schema initialized at {DbPath}", dbPath);
    }
}
