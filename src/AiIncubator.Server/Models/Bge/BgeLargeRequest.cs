using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Bge;

/// <summary>
/// Request body for the internal BGE-Large embedding server.
/// Endpoint: <c>POST {EmbeddingServerUrl}/embed</c>
/// </summary>
public class BgeLargeRequest(IReadOnlyList<string> texts)
{
    /// <summary>
    /// Texts to embed, one vector returned per item in the same order.
    /// </summary>
    [JsonPropertyName("texts")]
    public IReadOnlyList<string> Texts { get; init; } = texts;
}
