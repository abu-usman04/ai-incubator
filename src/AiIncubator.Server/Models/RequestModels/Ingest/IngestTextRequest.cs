namespace AiIncubator.Server.Models.RequestModels.Ingest;

public class IngestTextRequest
{
    public string DocumentId { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public Dictionary<string, string>? Metadata { get; init; }
}
