using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Glm;

/// <summary>
/// Response body from the ZAI GLM chat completion API.
/// Contains the model's generated response, token usage statistics, and optional error information.
/// <see href="https://docs.z.ai/api-reference/llm/chat-completion"/>
/// </summary>
public class GlmChatResponse
{
    /// <summary>
    /// Task ID assigned by the platform for this completion request.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Request ID. If provided in the request, it is echoed back; otherwise platform-generated.
    /// </summary>
    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    /// <summary>
    /// Request creation time as a Unix timestamp in seconds.
    /// </summary>
    [JsonPropertyName("created")]
    public long? Created { get; set; }

    /// <summary>
    /// The model name used for this completion.
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// List of model responses. Typically contains a single choice.
    /// </summary>
    [JsonPropertyName("choices")]
    public List<GlmChoice> Choices { get; set; } = [];

    /// <summary>
    /// Token usage statistics returned when the model call ends.
    /// </summary>
    [JsonPropertyName("usage")]
    public GlmUsage? Usage { get; set; }

    /// <summary>
    /// Error information if the request failed. Present only on business-level errors
    /// when the HTTP status is still 200 but the model encountered an issue.
    /// </summary>
    [JsonPropertyName("error")]
    public GlmError? Error { get; set; }
}