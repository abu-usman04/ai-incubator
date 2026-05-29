namespace AiIncubator.Server.Tests.Services.Retrieval;

using FluentAssertions;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Retrieval;

public class RagPromptBuilderTests
{
    [Fact]
    public void Build_NoSources_StillReturnsPrompt_AndInstructsToSayIDontKnow()
    {
        var builder = new RagPromptBuilder();

        RagPrompt prompt = builder.Build("What is RAG?", Array.Empty<RetrievedChunk>());

        prompt.System.Should().ContainAny("don't know", "do not know");
        prompt.User.Should().Contain("What is RAG?");
    }

    [Fact]
    public void Build_WithSources_EmbedsNumberedContextBlocks()
    {
        var builder = new RagPromptBuilder();
        var sources = new[]
        {
            new RetrievedChunk("doc-1#0", "RAG combines retrieval with generation.", 0.9f, new Dictionary<string, string>()),
            new RetrievedChunk("doc-1#1", "It typically uses a vector database.", 0.8f, new Dictionary<string, string>())
        };

        RagPrompt prompt = builder.Build("What is RAG?", sources);

        prompt.User.Should().Contain("[1]");
        prompt.User.Should().Contain("RAG combines retrieval with generation.");
        prompt.User.Should().Contain("[2]");
        prompt.User.Should().Contain("It typically uses a vector database.");
        prompt.User.Should().Contain("What is RAG?");
    }
}
