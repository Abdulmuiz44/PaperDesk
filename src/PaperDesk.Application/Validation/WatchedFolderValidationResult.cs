namespace PaperDesk.Application.Validation;

public sealed record WatchedFolderValidationResult(bool IsValid, string? Error)
{
    public static WatchedFolderValidationResult Success() => new(true, null);

    public static WatchedFolderValidationResult Failure(string error) => new(false, error);
}
