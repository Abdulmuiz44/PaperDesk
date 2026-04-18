namespace PaperDesk.Domain.Enums;

public enum ActivityType
{
    FileDetected = 0,
    FileQueued = 1,
    FileAnalyzed = 2,
    FileSkipped = 3,
    OcrCompleted = 4,
    RenameSuggested = 5,
    MoveSuggested = 6,
    ActionApproved = 7,
    ActionApplied = 8,
    DuplicateDetected = 9,
    Failure = 10,
    SettingsChanged = 11,
}
