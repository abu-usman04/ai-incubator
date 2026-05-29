using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Glm;

/// <summary>
/// Configuration for the model's chain-of-thought reasoning (deep thinking).
/// Supported by GLM-4.5 series and above.
/// </summary>
public class GlmThinkingConfig
{
    /// <summary>
    /// Whether to enable the chain of thought.
    /// <c>"enabled"</c> activates reasoning; <c>"disabled"</c> turns it off.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "enabled";

    /// <summary>
    /// Controls whether to clear <c>reasoning_content</c> from previous conversation turns.
    /// </summary>
    [JsonPropertyName("clear_thinking")]
    public bool ClearThinking { get; set; }
}
