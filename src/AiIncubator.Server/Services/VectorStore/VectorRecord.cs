using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.VectorStore;

public class VectorRecord(RagDocument document, IReadOnlyList<float> vector)
{
    public RagDocument Document { get; init; } = document;
    public IReadOnlyList<float> Vector { get; init; } = vector;
}
