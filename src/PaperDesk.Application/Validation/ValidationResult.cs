namespace PaperDesk.Application.Validation;

public sealed record ValidationResult(bool IsValid, IReadOnlyCollection<string> Errors)
{
    public static ValidationResult Success { get; } = new(true, Array.Empty<string>());
}
