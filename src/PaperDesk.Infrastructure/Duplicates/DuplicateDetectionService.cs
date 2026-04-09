using PaperDesk.Application.Abstractions;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Infrastructure.Duplicates;

public sealed class DuplicateDetectionService : IDuplicateDetectionService
{
    public Task<IReadOnlyCollection<DuplicateGroup>> FindDuplicatesAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<DuplicateGroup>>(Array.Empty<DuplicateGroup>());
}
