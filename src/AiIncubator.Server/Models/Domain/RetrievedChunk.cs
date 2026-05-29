namespace AiIncubator.Server.Models.Domain;

public class RetrievedChunk(
    string documentId,
    string content,
    float score,
    IReadOnlyDictionary<string, string> metadata)
{
    public string DocumentId { get; init; } = documentId;
    public string Content { get; init; } = content;
    public float Score { get; init; } = score;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = metadata;
}