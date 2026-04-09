using PaperDesk.Domain.Entities;

namespace PaperDesk.Application.Abstractions;

/// <summary>
/// Scaffold-stage repository abstraction for rename suggestion review queue operations.
/// </summary>
public interface IRenameSuggestionRepository
{
    Task<IReadOnlyCollection<RenameSuggestion>> ListForReviewQueueAsync(CancellationToken cancellationToken);

    Task AddAsync(RenameSuggestion suggestion, CancellationToken cancellationToken);

    Task UpdateAsync(RenameSuggestion suggestion, CancellationToken cancellationToken);
}
