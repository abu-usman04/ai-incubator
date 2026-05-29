using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Glm;

namespace AiIncubator.Server.Services.Chat;

/// <summary>
/// OpenAI-compatible chat client for GLM.
/// </summary>
/// <remarks>
/// Default endpoint is <c>https://api.z.ai/api/paas/v4/</c>. Works against any provider
/// that exposes the same chat-completions wire format. The full GLM contract
/// (tools, thinking, reasoning_content) is available via <see cref="ChatAsync"/>;
/// the high-level facade <see cref="CompleteAsync"/> is used by the RAG layer.
/// </remarks>
public class GlmChatClient(HttpClient httpClient, IOptions<ChatOptions> options) : IChatCompletionClient
{
    #region Private fields region

    private const string CompletionsPath = "chat/completions";
    private readonly ChatOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #endregion

    #region Public methods region

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var request = new GlmChatRequest
        {
            Model = _options.Model,
            Messages =
            [
                new GlmMessage
                {
                    Role = "system",
                    Content = systemPrompt
                },
                new GlmMessage
                {
                    Role = "user",
                    Content = userPrompt
                }
            ]
        };

        GlmChatResponse response = await ChatAsync(request, cancellationToken);
        return response.Choices.FirstOrDefault()?.Message.Content
               ?? throw new AppException(
                   HttpStatusCode.InternalServerError,
                   "Chat server returned no message content.",
                   errorCode: "INTERNAL_ERROR");
    }

    public async Task<GlmChatResponse> ChatAsync(GlmChatRequest request, CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, CompletionsPath)
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new AppException(
                HttpStatusCode.InternalServerError,
                $"Chat server request failed: {ex.Message}",
                "INTERNAL_ERROR",
                ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AppException(
                HttpStatusCode.InternalServerError,
                $"Chat server returned {(int)response.StatusCode}: {Truncate(body)}",
                "INTERNAL_ERROR");
        }

        GlmChatResponse? payload =
            await response.Content.ReadFromJsonAsync<GlmChatResponse>(JsonOptions, cancellationToken);
        if (payload is null || payload.Choices.Count == 0)
        {
            throw new AppException(
                HttpStatusCode.InternalServerError,
                "Chat server returned no choices.",
                "INTERNAL_ERROR");
        }

        if (payload.Error is not null)
        {
            throw new AppException(
                HttpStatusCode.InternalServerError,
                $"Chat server business error [{payload.Error.Code}]: {payload.Error.Message}",
                "INTERNAL_ERROR");
        }

        return payload;
    }

    #endregion

    #region Private methods

    private static string Truncate(string value)
    {
        const int MaxLength = 200;
        return value.Length <= MaxLength ? value : value[..MaxLength];
    }

    #endregion
}