using PaperDesk.Application.Validation;

namespace PaperDesk.Tests.Application;

public sealed class ValidationResultTests
{
    [Fact]
    public void SuccessResultIsValid()
    {
        Assert.True(ValidationResult.Success.IsValid);
    }
}
