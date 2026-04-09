using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Infrastructure.Persistence;

/// <summary>
/// Thin scaffold adapter for rename suggestion persistence/review queue operations.
/// </summary>
public sealed class SqliteRenameSuggestionRepository : IRenameSuggestionRepository
{
    public Task<IReadOnlyCollection<RenameSuggestion>> ListForReviewQueueAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<RenameSuggestion>>(Array.Empty<RenameSuggestion>());

    public Task AddAsync(RenameSuggestion suggestion, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task UpdateAsync(RenameSuggestion suggestion, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
