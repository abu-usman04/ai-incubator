using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.VectorStore;

public interface IVectorStore
{
    Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken);

    Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken);

    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(IReadOnlyList<float> queryVector, int topK,
        CancellationToken cancellationToken);

    Task DeleteByDocumentAsync(string sourceDocumentId, CancellationToken cancellationToken);
}