using PaperDesk.Domain.ValueObjects;
using PaperDesk.Infrastructure.Services;

namespace PaperDesk.Tests.Infrastructure;

public sealed class RenamingServiceTests
{
    [Fact]
    public async Task BuildPreviewAsyncCreatesTemplateBasedPreviewWithoutChangingOriginalFile()
    {
        using var temp = new TempDirectory();
        var filePath = Path.Combine(temp.Path, "scan001.pdf");
        await File.WriteAllTextAsync(filePath, "sample");
        var metadata = new DocumentMetadata(filePath, "scan001.pdf", ".pdf", 6, DateTimeOffset.UtcNow, new DateTimeOffset(2026, 4, 18, 0, 0, 0, TimeSpan.Zero));
        var service = new RenamingService();

        var preview = await service.BuildPreviewAsync(metadata, null, CancellationToken.None);

        Assert.Equal(filePath, preview.OriginalPath);
        Assert.Equal("2026-04-18_other_scan001.pdf", preview.ProposedFileName);
        Assert.True(File.Exists(filePath));
        Assert.False(File.Exists(preview.ProposedPath));
    }

    [Fact]
    public async Task BuildPreviewAsyncAvoidsExistingTargetNameConflicts()
    {
        using var temp = new TempDirectory();
        var filePath = Path.Combine(temp.Path, "scan001.pdf");
        var conflictPath = Path.Combine(temp.Path, "2026-04-18_other_scan001.pdf");
        await File.WriteAllTextAsync(filePath, "sample");
        await File.WriteAllTextAsync(conflictPath, "existing");
        var metadata = new DocumentMetadata(filePath, "scan001.pdf", ".pdf", 6, DateTimeOffset.UtcNow, new DateTimeOffset(2026, 4, 18, 0, 0, 0, TimeSpan.Zero));
        var service = new RenamingService();

        var preview = await service.BuildPreviewAsync(metadata, null, CancellationToken.None);

        Assert.Equal("2026-04-18_other_scan001 (2).pdf", preview.ProposedFileName);
        Assert.True(File.Exists(filePath));
    }
}
