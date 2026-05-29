using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Embeddings;
using AiIncubator.Server.Services.VectorStore;

namespace AiIncubator.Server.Services.Ingestion;

public class IngestionService(
    ITextSplitter splitter,
    IEmbeddingClient embeddings,
    IVectorStore vectorStore,
    IOptions<EmbeddingOptions> embeddingOptions) : IIngestionService
{
    #region Private fields region

    private const string ChunkIndexKey = "chunk_index";
    private readonly int _vectorSize = embeddingOptions.Value.VectorSize;

    #endregion

    #region Public methods region

    public async Task<int> IngestAsync(
        string documentId,
        string text,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new AppException(HttpStatusCode.BadRequest, "documentId is required.", "BAD_REQUEST");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new AppException(HttpStatusCode.BadRequest, "text is required.", "BAD_REQUEST");
        }

        await vectorStore.EnsureCollectionAsync(_vectorSize, cancellationToken);
        IReadOnlyList<string> chunks = splitter.Split(text);
        IReadOnlyList<float[]> vectors = await embeddings.EmbedBatchAsync(chunks, cancellationToken);
        IReadOnlyList<VectorRecord> records = BuildRecords(documentId, chunks, vectors, metadata);
        
        await vectorStore.UpsertAsync(records, cancellationToken);
        return chunks.Count;
    }

    #endregion

    #region Private methods region

    private static IReadOnlyList<VectorRecord> BuildRecords(
        string documentId,
        IReadOnlyList<string> chunks,
        IReadOnlyList<float[]> vectors,
        IReadOnlyDictionary<string, string> metadata)
    {
        var records = new List<VectorRecord>(chunks.Count);
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunkMetadata = new Dictionary<string, string>(metadata)
            {
                [ChunkIndexKey] = i.ToString(CultureInfo.InvariantCulture)
            };
            string chunkId = $"{documentId}#{i}";
            records.Add(new VectorRecord(
                new RagDocument(chunkId, chunks[i], chunkMetadata),
                vectors[i]));
        }

        return records;
    }

    #endregion
}