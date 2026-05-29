namespace AiIncubator.Server.Tests.Services.Documents;

using System.Text;
using FluentAssertions;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Services.Documents;

public class DocumentTextExtractorTests
{
    private readonly DocumentTextExtractor _extractor = new();

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("readme.md")]
    public async Task ExtractAsync_TextFile_ReturnsContent(string fileName)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));

        string text = await _extractor.ExtractAsync(stream, fileName, CancellationToken.None);

        text.Should().Be("hello world");
    }

    [Fact]
    public async Task ExtractAsync_UnsupportedExtension_ThrowsBadRequest()
    {
        using var stream = new MemoryStream([1, 2, 3]);

        Func<Task> act = () => _extractor.ExtractAsync(stream, "image.png", CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("BAD_REQUEST");
    }
}
