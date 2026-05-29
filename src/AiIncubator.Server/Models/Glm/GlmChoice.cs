using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Glm;

/// <summary>
/// A single choice in the model's response.
/// Contains the generated message and the reason generation stopped.
/// </summary>
public class GlmChoice
{
    /// <summary>Result index within the choices array.</summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>The model's generated message for this choice.</summary>
    [JsonPropertyName("message")]
    public GlmMessage Message { get; set; } = new();

    /// <summary>
    /// Reason for model inference termination: <c>"stop"</c>, <c>"tool_calls"</c>, <c>"length"</c>,
    /// <c>"sensitive"</c>, <c>"model_context_window_exceeded"</c>, <c>"network_error"</c>.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}
