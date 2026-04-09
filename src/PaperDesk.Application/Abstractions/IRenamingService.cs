using PaperDesk.Application.DTOs;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Application.Abstractions;

public interface IRenamingService
{
    Task<RenamePlanDto> BuildSuggestionAsync(DocumentRecord record, CancellationToken cancellationToken);
}
