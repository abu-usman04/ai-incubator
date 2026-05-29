namespace AiIncubator.Server.Tests.Services.Ingestion;

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Embeddings;
using AiIncubator.Server.Services.Ingestion;
using AiIncubator.Server.Services.VectorStore;

public class IngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_SplitsEmbedsAndUpserts_ReturningChunkCount()
    {
        Mock<ITextSplitter> splitter = new();
        Mock<IEmbeddingClient> embeddings = new();
        Mock<IVectorStore> vectorStore = new();

        splitter.Setup(s => s.Split("hello world")).Returns(new[] { "hello", "world" });
        embeddings.Setup(e => e.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new[] { 1f, 0f }, new[] { 0f, 1f } });

        IngestionService service = Create(splitter.Object, embeddings.Object, vectorStore.Object, vectorSize: 2);

        int count = await service.IngestAsync(
            documentId: "doc-1",
            text: "hello world",
            metadata: new Dictionary<string, string>(),
            cancellationToken: CancellationToken.None);

        count.Should().Be(2);
        vectorStore.Verify(v => v.EnsureCollectionAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        vectorStore.Verify(v => v.UpsertAsync(
            It.Is<IReadOnlyList<VectorRecord>>(records => records.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_DerivesChunkIdsFromDocumentIdAndIndex()
    {
        Mock<ITextSplitter> splitter = new();
        Mock<IEmbeddingClient> embeddings = new();
        Mock<IVectorStore> vectorStore = new();

        splitter.Setup(s => s.Split(It.IsAny<string>())).Returns(new[] { "a", "b", "c" });
        embeddings.Setup(e => e.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new[] { 1f, 0f }, new[] { 0f, 1f }, new[] { 1f, 1f } });

        IReadOnlyList<VectorRecord>? captured = null;
        vectorStore.Setup(v => v.UpsertAsync(It.IsAny<IReadOnlyList<VectorRecord>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<VectorRecord>, CancellationToken>((records, _) => captured = records)
            .Returns(Task.CompletedTask);

        IngestionService service = Create(splitter.Object, embeddings.Object, vectorStore.Object, vectorSize: 2);

        await service.IngestAsync("doc-42", "anything", new Dictionary<string, string>(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Select(r => r.Document.Id).Should().Equal("doc-42#0", "doc-42#1", "doc-42#2");
    }

    [Fact]
    public async Task IngestAsync_PropagatesMetadataAndAddsChunkIndex()
    {
        Mock<ITextSplitter> splitter = new();
        Mock<IEmbeddingClient> embeddings = new();
        Mock<IVectorStore> vectorStore = new();

        splitter.Setup(s => s.Split(It.IsAny<string>())).Returns(new[] { "a", "b" });
        embeddings.Setup(e => e.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new[] { 1f, 0f }, new[] { 0f, 1f } });

        IReadOnlyList<VectorRecord>? captured = null;
        vectorStore.Setup(v => v.UpsertAsync(It.IsAny<IReadOnlyList<VectorRecord>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<VectorRecord>, CancellationToken>((records, _) => captured = records)
            .Returns(Task.CompletedTask);

        IngestionService service = Create(splitter.Object, embeddings.Object, vectorStore.Object, vectorSize: 2);

        await service.IngestAsync(
            "doc-1",
            "text",
            new Dictionary<string, string> { ["source"] = "manual", ["author"] = "alice" },
            CancellationToken.None);

        captured![0].Document.Metadata.Should().Contain("source", "manual");
        captured[0].Document.Metadata.Should().Contain("author", "alice");
        captured[0].Document.Metadata.Should().Contain("chunk_index", "0");
        captured[1].Document.Metadata.Should().Contain("chunk_index", "1");
    }

    [Theory]
    [InlineData("", "some text")]
    [InlineData("  ", "some text")]
    [InlineData("doc-1", "")]
    [InlineData("doc-1", "   ")]
    public async Task IngestAsync_RejectsBlankInputs(string documentId, string text)
    {
        IngestionService service = Create(
            Mock.Of<ITextSplitter>(),
            Mock.Of<IEmbeddingClient>(),
            Mock.Of<IVectorStore>(),
            vectorSize: 2);

        Func<Task> act = () => service.IngestAsync(
            documentId, text, new Dictionary<string, string>(), CancellationToken.None);

        AppException ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.ErrorCode.Should().Be("BAD_REQUEST");
        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static IngestionService Create(
        ITextSplitter splitter,
        IEmbeddingClient embeddings,
        IVectorStore vectorStore,
        int vectorSize)
    {
        IOptions<EmbeddingOptions> options = Options.Create(new EmbeddingOptions
        {
            ServerUrl = "http://test/",
            VectorSize = vectorSize
        });
        return new IngestionService(splitter, embeddings, vectorStore, options);
    }
}
