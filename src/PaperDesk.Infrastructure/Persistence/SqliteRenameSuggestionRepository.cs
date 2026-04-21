using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;
using PaperDesk.Infrastructure.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace PaperDesk.Infrastructure.Persistence;

public sealed class SqliteRenameSuggestionRepository(
    SqlitePathResolver pathResolver,
    IOptions<AppSettings> appSettings) : IRenameSuggestionRepository
{
    public async Task<IReadOnlyCollection<RenameSuggestion>> ListForReviewQueueAsync(CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                id,
                document_id,
                proposed_file_name,
                proposed_destination_directory,
                confidence,
                reason,
                is_approved,
                is_skipped
            FROM rename_suggestions
            WHERE is_approved = 0 AND is_skipped = 0
            ORDER BY rowid DESC;
            """;

        var list = new List<RenameSuggestion>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new RenameSuggestion
            {
                Id = Guid.Parse(reader.GetString(0)),
                DocumentId = Guid.Parse(reader.GetString(1)),
                ProposedFileName = reader.GetString(2),
                ProposedDestinationDirectory = reader.IsDBNull(3) ? null : reader.GetString(3),
                Confidence = (PaperDesk.Domain.Enums.ConfidenceLevel)reader.GetInt32(4),
                Reason = reader.GetString(5),
                IsApproved = reader.GetInt32(6) == 1,
                IsSkipped = reader.GetInt32(7) == 1
            });
        }

        return list;
    }

    public async Task AddAsync(RenameSuggestion suggestion, CancellationToken cancellationToken)
    {
        await UpsertAsync(suggestion, cancellationToken);
    }

    public async Task UpdateAsync(RenameSuggestion suggestion, CancellationToken cancellationToken)
    {
        await UpsertAsync(suggestion, cancellationToken);
    }

    private async Task UpsertAsync(RenameSuggestion suggestion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO rename_suggestions (
                id,
                document_id,
                proposed_file_name,
                proposed_destination_directory,
                confidence,
                reason,
                is_approved,
                is_skipped
            ) VALUES (
                $id,
                $document_id,
                $proposed_file_name,
                $proposed_destination_directory,
                $confidence,
                $reason,
                $is_approved,
                $is_skipped
            )
            ON CONFLICT(id) DO UPDATE SET
                proposed_file_name = excluded.proposed_file_name,
                proposed_destination_directory = excluded.proposed_destination_directory,
                confidence = excluded.confidence,
                reason = excluded.reason,
                is_approved = excluded.is_approved,
                is_skipped = excluded.is_skipped;
            """;
        command.Parameters.AddWithValue("$id", suggestion.Id.ToString("D"));
        command.Parameters.AddWithValue("$document_id", suggestion.DocumentId.ToString("D"));
        command.Parameters.AddWithValue("$proposed_file_name", suggestion.ProposedFileName);
        command.Parameters.AddWithValue("$proposed_destination_directory", (object?)suggestion.ProposedDestinationDirectory ?? DBNull.Value);
        command.Parameters.AddWithValue("$confidence", (int)suggestion.Confidence);
        command.Parameters.AddWithValue("$reason", suggestion.Reason);
        command.Parameters.AddWithValue("$is_approved", suggestion.IsApproved ? 1 : 0);
        command.Parameters.AddWithValue("$is_skipped", suggestion.IsSkipped ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
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
}
