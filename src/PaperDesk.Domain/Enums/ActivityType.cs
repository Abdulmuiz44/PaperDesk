namespace PaperDesk.Domain.Enums;

public enum ActivityType
{
    FileDetected = 0,
    OcrCompleted = 1,
    RenameSuggested = 2,
    MoveSuggested = 3,
    ActionApproved = 4,
    ActionApplied = 5,
    DuplicateDetected = 6,
    Failure = 7,
    SettingsChanged = 8,
}
