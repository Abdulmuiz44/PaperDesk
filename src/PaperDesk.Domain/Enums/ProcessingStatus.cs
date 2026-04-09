namespace PaperDesk.Domain.Enums;

public enum ProcessingStatus
{
    Pending = 0,
    Queued = 1,
    Processing = 2,
    NeedsReview = 3,
    Completed = 4,
    Failed = 5,
    Skipped = 6,
}
