using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Bge;

/// <summary>
/// Response body from the internal BGE-Large embedding server.
/// </summary>
public class BgeLargeResponse(IReadOnlyList<float[]> embeddings)
{
    /// <summary>
    /// Embedding vectors. Each vector must have length equal to <c>Embedding:VectorSize</c>.
    /// </summary>
    [JsonPropertyName("embeddings")]
    public IReadOnlyList<float[]> Embeddings { get; init; } = embeddings;
}
