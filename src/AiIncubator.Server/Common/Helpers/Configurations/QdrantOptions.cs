namespace AiIncubator.Server.Common.Helpers.Configurations;

public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 6334;

    public bool Https { get; init; } = false;

    public string? ApiKey { get; init; }

    public string CollectionName { get; init; } = "naiton-rag";
}
