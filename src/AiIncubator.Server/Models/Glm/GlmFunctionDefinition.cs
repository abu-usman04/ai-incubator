using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Glm;

/// <summary>
/// Describes a callable function within a <see cref="GlmToolDefinition"/>.
/// </summary>
public class GlmFunctionDefinition
{
    /// <summary>The name of the function to be called.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>A description of what the function does.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Parameters defined using JSON Schema.</summary>
    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new { };
}
