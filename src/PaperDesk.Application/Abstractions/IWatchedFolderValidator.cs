using PaperDesk.Application.Validation;
using PaperDesk.Domain.Entities;

namespace PaperDesk.Application.Abstractions;

public interface IWatchedFolderValidator
{
    WatchedFolderValidationResult Validate(WatchedFolder folder);
}
