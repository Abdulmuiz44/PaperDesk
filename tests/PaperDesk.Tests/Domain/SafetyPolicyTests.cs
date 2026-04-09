using PaperDesk.Domain.Policies;

namespace PaperDesk.Tests.Domain;

public sealed class SafetyPolicyTests
{
    [Fact]
    public void BulkActionsRequireExplicitApproval()
    {
        Assert.True(SafetyPolicy.RequiresExplicitApprovalForBulkActions);
    }

    [Fact]
    public void AutomaticDeletionIsDisabled()
    {
        Assert.False(SafetyPolicy.AllowAutomaticDeletion);
    }
}
