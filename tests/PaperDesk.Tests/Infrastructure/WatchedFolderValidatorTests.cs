using PaperDesk.Domain.Entities;
using PaperDesk.Infrastructure.Validation;

namespace PaperDesk.Tests.Infrastructure;

public sealed class WatchedFolderValidatorTests
{
    [Fact]
    public void ValidateAcceptsExistingNonSystemFolder()
    {
        using var temp = new TempDirectory();
        var validator = new WatchedFolderValidator();

        var result = validator.Validate(new WatchedFolder { Path = temp.Path });

        Assert.True(result.IsValid, result.Error);
    }

    [Fact]
    public void ValidateRejectsMissingFolder()
    {
        var validator = new WatchedFolderValidator();
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var result = validator.Validate(new WatchedFolder { Path = missingPath });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateRejectsDriveRoot()
    {
        var validator = new WatchedFolderValidator();
        var root = Path.GetPathRoot(Environment.SystemDirectory)!;

        var result = validator.Validate(new WatchedFolder { Path = root });

        Assert.False(result.IsValid);
    }
}
