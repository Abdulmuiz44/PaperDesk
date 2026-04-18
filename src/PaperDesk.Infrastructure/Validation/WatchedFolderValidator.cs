using PaperDesk.Application.Abstractions;
using PaperDesk.Application.Validation;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Infrastructure.Validation;

public sealed class WatchedFolderValidator : IWatchedFolderValidator
{
    public WatchedFolderValidationResult Validate(WatchedFolder folder)
    {
        if (string.IsNullOrWhiteSpace(folder.Path))
        {
            return WatchedFolderValidationResult.Failure("Watched folder path is required.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(folder.Path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return WatchedFolderValidationResult.Failure("Watched folder path is invalid.");
        }

        if (!Directory.Exists(fullPath))
        {
            return WatchedFolderValidationResult.Failure("Watched folder does not exist.");
        }

        var root = Path.GetPathRoot(fullPath);
        if (string.Equals(root?.TrimEnd(Path.DirectorySeparatorChar), fullPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            return WatchedFolderValidationResult.Failure("Watching a drive root is not allowed.");
        }

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrWhiteSpace(windows) && IsSameOrChildOf(fullPath, windows))
        {
            return WatchedFolderValidationResult.Failure("Watching the Windows system folder is not allowed.");
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles) && IsSameOrChildOf(fullPath, programFiles))
        {
            return WatchedFolderValidationResult.Failure("Watching Program Files is not allowed.");
        }

        return WatchedFolderValidationResult.Success();
    }

    private static bool IsSameOrChildOf(string candidate, string parent)
    {
        var normalizedCandidate = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
    }
}
