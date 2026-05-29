using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Bge;

/// <summary>
/// Response body from the internal BGE-Large embedding server.
/// </summary>
public class BgeLargeResponse(IReadOnlyList<float[]> dense)
{
    /// <summary>
    /// Dense embedding vectors. Each vector must have length equal to <c>Embedding:VectorSize</c>.
    /// </summary>
    [JsonPropertyName("dense")]
    public IReadOnlyList<float[]> Dense { get; init; } = dense;
}
