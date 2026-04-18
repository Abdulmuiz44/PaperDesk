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
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("SQLite schema initialized at {DbPath}", dbPath);
    }
}
