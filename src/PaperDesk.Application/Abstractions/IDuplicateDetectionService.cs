using PaperDesk.Domain.Entities;

namespace PaperDesk.Application.Abstractions;

public interface IDuplicateDetectionService
{
    Task<IReadOnlyCollection<DuplicateGroup>> FindDuplicatesAsync(CancellationToken cancellationToken);
}
