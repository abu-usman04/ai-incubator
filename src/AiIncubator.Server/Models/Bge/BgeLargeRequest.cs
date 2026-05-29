using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Bge;

/// <summary>
/// Request body for the internal BGE-Large embedding server.
/// Endpoint: <c>POST {EmbeddingServerUrl}/embed</c>
/// </summary>
public class BgeLargeRequest(IReadOnlyList<string> sentences)
{
    /// <summary>
    /// Sentences to embed, one vector returned per item in the same order.
    /// </summary>
    [JsonPropertyName("sentences")]
    public IReadOnlyList<string> Sentences { get; init; } = sentences;
}
