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
        var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("SQLite foundation initialized at {DbPath}", dbPath);
    }
}
