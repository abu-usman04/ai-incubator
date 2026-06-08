namespace AiIncubator.Server.Tests.Services.Retrieval;

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Chat;
using AiIncubator.Server.Services.Embeddings;
using AiIncubator.Server.Services.Retrieval;
using AiIncubator.Server.Services.VectorStore;

public class QueryServiceTests
{
    [Fact]
    public async Task AnswerAsync_EmbedsQuestion_Searches_PromptsAndReturnsAnswer()
    {
        Mock<IEmbeddingClient> embeddings = new();
        Mock<IVectorStore> vectorStore = new();
        Mock<IChatCompletionClient> chat = new();

        embeddings.Setup(e => e.EmbedAsync("what is rag?", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1f, 0f });
        var hits = new[]
        {
            new RetrievedChunk("doc-1#0", "RAG retrieves context.", 0.9f, new Dictionary<string, string>())
        };
        vectorStore.Setup(v => v.SearchAsync(It.IsAny<IReadOnlyList<float>>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hits);
        chat.Setup(c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("RAG is retrieval-augmented generation.");

        QueryService service = Create(embeddings.Object, vectorStore.Object, chat.Object, defaultTopK: 4);

        RagAnswer answer = await service.AnswerAsync("what is rag?", topK: 0, CancellationToken.None);

        answer.Answer.Should().Be("RAG is retrieval-augmented generation.");
        answer.Sources.Should().BeEquivalentTo(hits);
    }

    [Fact]
    public async Task RetrieveAsync_EmbedsQueryAndReturnsChunks_WithoutCallingChat()
    {
        Mock<IEmbeddingClient> embeddings = new();
        Mock<IVectorStore> vectorStore = new();
        Mock<IChatCompletionClient> chat = new();

        embeddings.Setup(e => e.EmbedAsync("onboarding", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1f, 0f });
        var hits = new[]
        {
            new RetrievedChunk("doc-1#0", "Onboarding context.", 0.8f, new Dictionary<string, string>())
        };
        vectorStore.Setup(v => v.SearchAsync(It.IsAny<IReadOnlyList<float>>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hits);

        QueryService service = Create(embeddings.Object, vectorStore.Object, chat.Object, defaultTopK: 4);

        IReadOnlyList<RetrievedChunk> chunks = await service.RetrieveAsync("onboarding", topK: 5, CancellationToken.None);

        chunks.Should().BeEquivalentTo(hits);
        chat.Verify(c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RetrieveAsync_BlankQuery_Throws()
    {
        QueryService service = Create(
            Mock.Of<IEmbeddingClient>(),
            Mock.Of<IVectorStore>(),
            Mock.Of<IChatCompletionClient>(),
            defaultTopK: 4);

        Func<Task> act = () => service.RetrieveAsync("   ", topK: 4, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("BAD_REQUEST");
    }

    [Fact]
    public async Task AnswerAsync_EmptySources_ReturnsIDontKnowWithoutCallingChat()
    {
        Mock<IEmbeddingClient> embeddings = new();
        Mock<IVectorStore> vectorStore = new();
        Mock<IChatCompletionClient> chat = new();

        embeddings.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1f, 0f });
        vectorStore.Setup(v => v.SearchAsync(It.IsAny<IReadOnlyList<float>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RetrievedChunk>());

        QueryService service = Create(embeddings.Object, vectorStore.Object, chat.Object, defaultTopK: 4);

        RagAnswer answer = await service.AnswerAsync("anything", topK: 4, CancellationToken.None);

        answer.Answer.Should().Be("I don't know.");
        answer.Sources.Should().BeEmpty();
        chat.Verify(c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnswerAsync_BlankQuestion_Throws(string question)
    {
        QueryService service = Create(
            Mock.Of<IEmbeddingClient>(),
            Mock.Of<IVectorStore>(),
            Mock.Of<IChatCompletionClient>(),
            defaultTopK: 4);

        Func<Task> act = () => service.AnswerAsync(question, topK: 4, CancellationToken.None);

        AppException ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.ErrorCode.Should().Be("BAD_REQUEST");
        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AnswerAsync_NonPositiveTopK_UsesDefault()
    {
        Mock<IEmbeddingClient> embeddings = new();
        Mock<IVectorStore> vectorStore = new();
        Mock<IChatCompletionClient> chat = new();

        embeddings.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1f });
        vectorStore.Setup(v => v.SearchAsync(It.IsAny<IReadOnlyList<float>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RetrievedChunk>());

        QueryService service = Create(embeddings.Object, vectorStore.Object, chat.Object, defaultTopK: 7);

        await service.AnswerAsync("q", topK: 0, CancellationToken.None);

        vectorStore.Verify(v => v.SearchAsync(It.IsAny<IReadOnlyList<float>>(), 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnswerAsync_TopKAboveMax_IsClampedTo20()
    {
        Mock<IEmbeddingClient> embeddings = new();
        Mock<IVectorStore> vectorStore = new();
        Mock<IChatCompletionClient> chat = new();

        embeddings.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1f });
        vectorStore.Setup(v => v.SearchAsync(It.IsAny<IReadOnlyList<float>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RetrievedChunk>());

        QueryService service = Create(embeddings.Object, vectorStore.Object, chat.Object, defaultTopK: 4);

        await service.AnswerAsync("q", topK: 9999, CancellationToken.None);

        vectorStore.Verify(v => v.SearchAsync(It.IsAny<IReadOnlyList<float>>(), 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static QueryService Create(
        IEmbeddingClient embeddings,
        IVectorStore vectorStore,
        IChatCompletionClient chat,
        int defaultTopK)
    {
        IOptions<RagOptions> options = Options.Create(new RagOptions
        {
            ChunkSize = 800,
            ChunkOverlap = 100,
            DefaultTopK = defaultTopK
        });
        return new QueryService(embeddings, vectorStore, chat, new RagPromptBuilder(), options);
    }
}
