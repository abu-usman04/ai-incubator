namespace AiIncubator.Server.Services.Ingestion;

public interface IIngestionService
{
    Task<int> IngestAsync(
        string documentId,
        string text,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken);
}
