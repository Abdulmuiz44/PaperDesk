namespace PaperDesk.Domain.Policies;

public static class SafetyPolicy
{
    public static bool RequiresExplicitApprovalForBulkActions => true;

    public static bool AllowAutomaticDeletion => false;
}
