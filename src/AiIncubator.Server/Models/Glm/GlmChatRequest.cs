using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Glm;

/// <summary>
/// Request body for the ZhipuAI GLM chat completion API.
/// Endpoint: <c>POST https://api.z.ai/api/paas/v4/chat/completions</c>
/// <see href="https://docs.z.ai/api-reference/llm/chat-completion"/>
/// </summary>
public class GlmChatRequest
{
    /// <summary>
    /// The model to be called.
    /// GLM-5.1, GLM-5 are the latest flagship model series,
    /// foundational models specifically designed for agent applications.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The current conversation message list as the model's prompt input, provided in JSON array format.
    /// Must contain at least one message. The input must not consist of system messages
    /// or assistant messages only.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<GlmMessage> Messages { get; set; } = [];

    /// <summary>
    /// A list of tools the model may call. Currently only functions are supported as a tool.
    /// Use this to provide a list of functions the model may generate JSON inputs for.
    /// Maximum of 128 tools per request.
    /// </summary>
    [JsonPropertyName("tools")]
    public List<GlmToolDefinition>? Tools { get; set; }

    /// <summary>
    /// Controls how the model selects which function to call.
    /// Only applicable when the tool type is <c>"function"</c>.
    /// Currently only <c>"auto"</c> is supported â€” the model decides whether to call tools.
    /// </summary>
    [JsonPropertyName("tool_choice")]
    public string ToolChoice { get; set; } = "auto";

    /// <summary>
    /// Configuration for chain-of-thought reasoning (deep thinking).
    /// Supported by GLM-4.5 series and above.
    /// When <c>null</c>, the model uses its default thinking behavior.
    /// </summary>
    [JsonPropertyName("thinking")]
    public GlmThinkingConfig? Thinking { get; set; }

    
    //todo set the temperature from settings 
    [JsonPropertyName("temperature")] 
    public double Temperature { get; set; }

    /// <summary>
    /// Maximum number of tokens for model output. Range: [1, 131072].
    /// GLM-5.1, GLM-5, GLM-4.7, GLM-4.6 series support 128K maximum output.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 16384;

    /// <summary>
    /// When <c>false</c> (default), the model returns all content at once after generation completes.
    /// When <c>true</c>, the model streams content via standard Event Stream (SSE),
    /// ending with a <c>data: [DONE]</c> message.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    /// <summary>
    /// When <c>true</c> (default), sampling strategy is enabled.
    /// When <c>false</c>, sampling parameters such as <see cref="Temperature"/>
    /// and top_p will not take effect (deterministic/greedy output).
    /// </summary>
    [JsonPropertyName("do_sample")]
    public bool DoSample { get; set; } = true;
}