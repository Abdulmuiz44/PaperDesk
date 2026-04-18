using PaperDesk.Infrastructure.Services;

namespace PaperDesk.Tests.Infrastructure;

public sealed class DocumentMetadataExtractorTests
{
    [Fact]
    public async Task ExtractAsyncReturnsBasicFileMetadata()
    {
        using var temp = new TempDirectory();
        var filePath = Path.Combine(temp.Path, "invoice.pdf");
        await File.WriteAllTextAsync(filePath, "sample");
        var extractor = new DocumentMetadataExtractor();

        var metadata = await extractor.ExtractAsync(filePath, CancellationToken.None);

        Assert.Equal(filePath, metadata.FullPath);
        Assert.Equal("invoice.pdf", metadata.FileName);
        Assert.Equal(".pdf", metadata.Extension);
        Assert.True(metadata.SizeBytes > 0);
    }

    [Fact]
    public async Task ExtractAsyncRetriesUntilLockedFileIsReadable()
    {
        using var temp = new TempDirectory();
        var filePath = Path.Combine(temp.Path, "receipt.pdf");
        await File.WriteAllTextAsync(filePath, "sample");
        var extractor = new DocumentMetadataExtractor();

        await using var locked = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var extraction = extractor.ExtractAsync(filePath, CancellationToken.None);
        await Task.Delay(250);
        await locked.DisposeAsync();

        var metadata = await extraction;

        Assert.Equal("receipt.pdf", metadata.FileName);
    }
}
