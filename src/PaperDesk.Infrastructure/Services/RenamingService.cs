using PaperDesk.Application.Abstractions;
using PaperDesk.Application.DTOs;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;

namespace PaperDesk.Infrastructure.Services;

public sealed class RenamingService : IRenamingService
{
    public Task<RenamePlanDto> BuildSuggestionAsync(DocumentRecord record, CancellationToken cancellationToken)
    {
        var suggestion = new RenamePlanDto(
            record.Id,
            Path.GetFileName(record.OriginalPath),
            Path.GetDirectoryName(record.OriginalPath),
            ConfidenceLevel.Low,
            "Placeholder suggestion; implementation deferred");

        return Task.FromResult(suggestion);
    }
}
