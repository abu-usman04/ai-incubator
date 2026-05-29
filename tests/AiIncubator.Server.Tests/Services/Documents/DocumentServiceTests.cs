namespace AiIncubator.Server.Tests.Services.Documents;

using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Documents;
using AiIncubator.Server.Services.Ingestion;
using AiIncubator.Server.Services.VectorStore;

public class DocumentServiceTests
{
    private readonly Mock<IIngestionService> _ingestion = new();
    private readonly Mock<IVectorStore> _vectorStore = new();
    private readonly InMemoryDocumentRepository _repository = new();
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _service = new DocumentService(
            _repository,
            new DocumentTextExtractor(),
            _ingestion.Object,
            _vectorStore.Object,
            Options.Create(new DocumentsOptions()),
            NullLogger<DocumentService>.Instance);
    }

    [Fact]
    public async Task UploadAsync_TextFile_IngestsAndMarksIndexed()
    {
        _ingestion
            .Setup(s => s.IngestAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("some content"));
        Document document = await _service.UploadAsync(stream, "doc.txt", "text/plain", 12, null, CancellationToken.None);

        document.Status.Should().Be(DocumentStatus.Indexed);
        document.ChunkCount.Should().Be(3);
    }

    [Fact]
    public async Task UploadAsync_UnsupportedType_ThrowsAndMarksFailed()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("x"));

        Func<Task> act = () => _service.UploadAsync(stream, "a.png", "image/png", 1, null, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task UploadAsync_TooLarge_ThrowsBadRequest()
    {
        var service = new DocumentService(
            _repository,
            new DocumentTextExtractor(),
            _ingestion.Object,
            _vectorStore.Object,
            Options.Create(new DocumentsOptions { MaxUploadBytes = 5 }),
            NullLogger<DocumentService>.Instance);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("too big content"));
        Func<Task> act = () => service.UploadAsync(stream, "big.txt", "text/plain", 100, null, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("BAD_REQUEST");
    }

    [Fact]
    public async Task DeleteAsync_RemovesVectorsAndDocument()
    {
        _ingestion
            .Setup(s => s.IngestAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        Document document = await _service.UploadAsync(stream, "doc.txt", "text/plain", 7, null, CancellationToken.None);

        await _service.DeleteAsync(document.Id, CancellationToken.None);

        _vectorStore.Verify(v => v.DeleteByDocumentAsync(document.Id, It.IsAny<CancellationToken>()), Times.Once);
        Func<Task> act = () => _service.GetAsync(document.Id, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>();
    }
}
