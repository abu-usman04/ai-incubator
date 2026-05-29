namespace AiIncubator.Server.Tests.Controllers;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Models.RequestModels.Ingest;
using AiIncubator.Server.Tests.TestHelpers;

public class IngestControllerTests : IClassFixture<RagWebApplicationFactory>
{
    private readonly RagWebApplicationFactory _factory;

    public IngestControllerTests(RagWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_ValidBody_Returns200WithChunkCount()
    {
        _factory.Ingestion.Reset();
        _factory.Ingestion
            .Setup(s => s.IngestAsync("doc-1", "hello", It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("api/ingest", new IngestTextRequest
        {
            DocumentId = "doc-1",
            Text = "hello",
            Metadata = new Dictionary<string, string> { ["source"] = "test" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiResponse<IngestTextResponse>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<IngestTextResponse>>();
        payload!.Success.Should().BeTrue();
        payload.Data!.DocumentId.Should().Be("doc-1");
        payload.Data.ChunkCount.Should().Be(7);
    }

    [Fact]
    public async Task Post_WhenServiceThrowsBadRequest_Returns400()
    {
        _factory.Ingestion.Reset();
        _factory.Ingestion
            .Setup(s => s.IngestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AppException(HttpStatusCode.BadRequest, "documentId is required.", "BAD_REQUEST"));

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("api/ingest", new IngestTextRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ApiResponse<object>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        payload!.Success.Should().BeFalse();
        payload.Error!.Code.Should().Be("BAD_REQUEST");
    }
}
