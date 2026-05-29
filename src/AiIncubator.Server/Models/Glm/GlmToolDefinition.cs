using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Glm;

/// <summary>
/// Defines a tool that the model may call during chat completion.
/// </summary>
public class GlmToolDefinition
{
    /// <summary>
    /// Designates the tool category.
    /// Supported values: <c>"function"</c>, <c>"retrieval"</c>, <c>"web_search"</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>Container for the function specification.</summary>
    [JsonPropertyName("function")]
    public GlmFunctionDefinition Function { get; set; } = new();
}
