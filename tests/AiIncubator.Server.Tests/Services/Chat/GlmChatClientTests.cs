namespace AiIncubator.Server.Tests.Services.Chat;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Glm;
using AiIncubator.Server.Services.Chat;
using AiIncubator.Server.Tests.TestHelpers;

public class GlmChatClientTests
{
    private const string BaseUrl = "https://glm.test/api/paas/v4/";

    [Fact]
    public async Task CompleteAsync_SendsCorrectRequestAndReturnsAssistantContent()
    {
        StubHttpMessageHandler handler = StubReturning(new GlmChatResponse
        {
            Choices =
            [
                new GlmChoice { Index = 0, Message = new GlmMessage { Role = "assistant", Content = "the answer" } }
            ]
        });
        IChatCompletionClient client = CreateClient(handler);

        string result = await client.CompleteAsync("system text", "user text", CancellationToken.None);

        result.Should().Be("the answer");

        HttpRequestMessage sent = handler.Requests.Should().ContainSingle().Subject;
        sent.Method.Should().Be(HttpMethod.Post);
        sent.RequestUri!.ToString().Should().Be($"{BaseUrl}chat/completions");
        sent.Headers.Authorization!.Scheme.Should().Be("Bearer");
        sent.Headers.Authorization.Parameter.Should().Be("test-key");

        GlmChatRequest? body = await sent.Content!.ReadFromJsonAsync<GlmChatRequest>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        body!.Model.Should().Be("glm-5.1");
        body.Messages.Should().HaveCount(2);
        body.Messages[0].Role.Should().Be("system");
        body.Messages[0].Content.Should().Be("system text");
        body.Messages[1].Role.Should().Be("user");
        body.Messages[1].Content.Should().Be("user text");
    }

    [Fact]
    public async Task CompleteAsync_OnNonSuccessStatus_ThrowsAppException()
    {
        StubHttpMessageHandler handler = new(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("invalid key")
        }));
        IChatCompletionClient client = CreateClient(handler);

        Func<Task> act = () => client.CompleteAsync("sys", "usr", CancellationToken.None);

        AppException ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.ErrorCode.Should().Be("INTERNAL_ERROR");
    }

    [Fact]
    public async Task CompleteAsync_OnEmptyChoices_ThrowsAppException()
    {
        StubHttpMessageHandler handler = StubReturning(new GlmChatResponse { Choices = [] });
        IChatCompletionClient client = CreateClient(handler);

        Func<Task> act = () => client.CompleteAsync("sys", "usr", CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("INTERNAL_ERROR");
    }

    [Fact]
    public async Task CompleteAsync_OnBusinessError_ThrowsAppException()
    {
        StubHttpMessageHandler handler = StubReturning(new GlmChatResponse
        {
            Choices =
            [
                new GlmChoice { Index = 0, Message = new GlmMessage { Role = "assistant", Content = "" } }
            ],
            Error = new GlmError { Code = "1301", Message = "Sensitive content detected." }
        });
        IChatCompletionClient client = CreateClient(handler);

        Func<Task> act = () => client.CompleteAsync("sys", "usr", CancellationToken.None);

        AppException ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.ErrorCode.Should().Be("INTERNAL_ERROR");
        ex.Message.Should().Contain("1301");
    }

    private static GlmChatClient CreateClient(StubHttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        IOptions<ChatOptions> options = Options.Create(new ChatOptions
        {
            BaseUrl = BaseUrl,
            ApiKey = "test-key",
            Model = "glm-5.1",
            TimeoutSeconds = 60
        });
        return new GlmChatClient(http, options);
    }

    private static StubHttpMessageHandler StubReturning(GlmChatResponse payload)
    {
        return new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
        }));
    }
}
