namespace AiIncubator.Server.Services.Embeddings;

public interface IEmbeddingClient
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken);

    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}
