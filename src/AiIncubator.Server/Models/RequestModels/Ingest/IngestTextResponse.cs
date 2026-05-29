namespace AiIncubator.Server.Models.RequestModels.Ingest;

public class IngestTextResponse(string documentId, int chunkCount)
{
    public string DocumentId { get; init; } = documentId;

    public int ChunkCount { get; init; } = chunkCount;
}
