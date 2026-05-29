namespace AiIncubator.Server.Models.Domain;

public class RagDocument(string id, string content, IReadOnlyDictionary<string, string> metadata)
{
    public string Id { get; init; } = id;

    public string Content { get; init; } = content;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = metadata;
}
