namespace PaperDesk.Application.Commands;

public sealed record ApproveSuggestionsCommand(IReadOnlyCollection<Guid> SuggestionIds);
