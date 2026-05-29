namespace AiIncubator.Server.Tests.Controllers;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.Query;
using AiIncubator.Server.Tests.TestHelpers;

public class QueryControllerTests : IClassFixture<RagWebApplicationFactory>
{
    private readonly RagWebApplicationFactory _factory;

    public QueryControllerTests(RagWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_ValidBody_Returns200WithAnswer()
    {
        _factory.Query.Reset();
        var hits = new[]
        {
            new RetrievedChunk("doc-1#0", "context", 0.9f, new Dictionary<string, string>())
        };
        _factory.Query
            .Setup(s => s.AnswerAsync("what is rag?", 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswer("RAG is...", hits));

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("api/query", new QueryRequest
        {
            Question = "what is rag?",
            TopK = 4
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiResponse<RagAnswer>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<RagAnswer>>();
        payload!.Success.Should().BeTrue();
        payload.Data!.Answer.Should().Be("RAG is...");
        payload.Data.Sources.Should().HaveCount(1);
    }

    [Fact]
    public async Task Post_BlankQuestion_Returns400ViaServiceException()
    {
        _factory.Query.Reset();
        _factory.Query
            .Setup(s => s.AnswerAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AppException(HttpStatusCode.BadRequest, "question is required.", "BAD_REQUEST"));

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("api/query", new QueryRequest { Question = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ApiResponse<object>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        payload!.Error!.Code.Should().Be("BAD_REQUEST");
    }
}
