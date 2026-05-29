using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Glm;

/// <summary>
/// Token usage statistics returned when the GLM model call ends.
/// </summary>
public class GlmUsage
{
    /// <summary>Number of tokens in the user input (prompt).</summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>Number of tokens in the model's output (completion).</summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>Total number of tokens (prompt + completion).</summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
