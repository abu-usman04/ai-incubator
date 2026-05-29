namespace AiIncubator.Server.Tests.Services.Embeddings;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Bge;
using AiIncubator.Server.Services.Embeddings;
using AiIncubator.Server.Tests.TestHelpers;

public class BgeLargeEmbeddingClientTests
{
    private const string BaseUrl = "http://bge.test/";

    [Fact]
    public async Task EmbedAsync_SendsPostWithSingleTextArray_AndReturnsFirstEmbedding()
    {
        StubHttpMessageHandler handler = StubReturning(new BgeLargeResponse(new[] { new[] { 0.1f, 0.2f, 0.3f } }));
        IEmbeddingClient client = CreateClient(handler);

        float[] result = await client.EmbedAsync("hello world", CancellationToken.None);

        result.Should().Equal(0.1f, 0.2f, 0.3f);

        HttpRequestMessage sent = handler.Requests.Should().ContainSingle().Subject;
        sent.Method.Should().Be(HttpMethod.Post);
        sent.RequestUri!.ToString().Should().Be($"{BaseUrl}embed");

        BgeLargeRequest? body = await sent.Content!.ReadFromJsonAsync<BgeLargeRequest>();
        body!.Sentences.Should().ContainSingle().Which.Should().Be("hello world");
    }

    [Fact]
    public async Task EmbedBatchAsync_PreservesOrder_AndReturnsAllEmbeddings()
    {
        StubHttpMessageHandler handler = StubReturning(new BgeLargeResponse(new[]
        {
            new[] { 1f, 0f, 0f },
            new[] { 0f, 1f, 0f },
            new[] { 0f, 0f, 1f }
        }));
        IEmbeddingClient client = CreateClient(handler);

        IReadOnlyList<float[]> result = await client.EmbedBatchAsync(
            new[] { "a", "b", "c" },
            CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].Should().Equal(1f, 0f, 0f);
        result[1].Should().Equal(0f, 1f, 0f);
        result[2].Should().Equal(0f, 0f, 1f);
    }

    [Fact]
    public async Task EmbedBatchAsync_EmptyInput_ReturnsEmptyWithoutHttpCall()
    {
        StubHttpMessageHandler handler = StubReturning(new BgeLargeResponse(Array.Empty<float[]>()));
        IEmbeddingClient client = CreateClient(handler);

        IReadOnlyList<float[]> result = await client.EmbedBatchAsync(Array.Empty<string>(), CancellationToken.None);

        result.Should().BeEmpty();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task EmbedAsync_OnNonSuccessStatus_ThrowsAppExceptionWithInternalErrorCode()
    {
        StubHttpMessageHandler handler = new(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("server boom")
        }));
        IEmbeddingClient client = CreateClient(handler);

        Func<Task> act = () => client.EmbedAsync("anything", CancellationToken.None);

        AppException ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.ErrorCode.Should().Be("INTERNAL_ERROR");
        ex.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private static BgeLargeEmbeddingClient CreateClient(StubHttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        IOptions<EmbeddingOptions> options = Options.Create(new EmbeddingOptions
        {
            ServerUrl = BaseUrl,
            TimeoutSeconds = 30,
            VectorSize = 3
        });
        return new BgeLargeEmbeddingClient(http, options);
    }

    private static StubHttpMessageHandler StubReturning(BgeLargeResponse payload)
    {
        return new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
        }));
    }
}
