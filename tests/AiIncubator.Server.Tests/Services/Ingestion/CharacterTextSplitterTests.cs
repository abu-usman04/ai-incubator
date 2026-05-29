namespace AiIncubator.Server.Tests.Services.Ingestion;

using FluentAssertions;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Services.Ingestion;

public class CharacterTextSplitterTests
{
    [Fact]
    public void Split_EmptyInput_ReturnsEmpty()
    {
        ITextSplitter splitter = Create(chunkSize: 10, chunkOverlap: 2);

        splitter.Split(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Split_InputShorterThanChunkSize_ReturnsSingleChunk()
    {
        ITextSplitter splitter = Create(chunkSize: 100, chunkOverlap: 10);

        splitter.Split("hello world").Should().Equal("hello world");
    }

    [Fact]
    public void Split_InputExactlyChunkSize_ReturnsSingleChunk()
    {
        ITextSplitter splitter = Create(chunkSize: 5, chunkOverlap: 1);

        splitter.Split("12345").Should().Equal("12345");
    }

    [Fact]
    public void Split_LongInput_ProducesOverlappingChunks()
    {
        ITextSplitter splitter = Create(chunkSize: 5, chunkOverlap: 2);

        IReadOnlyList<string> chunks = splitter.Split("ABCDEFGHIJKL");

        chunks.Should().Equal("ABCDE", "DEFGH", "GHIJK", "JKL");
    }

    [Fact]
    public void Split_OverlapEqualToSize_Throws()
    {
        Action act = () => Create(chunkSize: 5, chunkOverlap: 5);

        act.Should().Throw<ArgumentException>();
    }

    private static CharacterTextSplitter Create(int chunkSize, int chunkOverlap)
    {
        IOptions<RagOptions> options = Options.Create(new RagOptions
        {
            ChunkSize = chunkSize,
            ChunkOverlap = chunkOverlap,
            DefaultTopK = 4
        });
        return new CharacterTextSplitter(options);
    }
}
