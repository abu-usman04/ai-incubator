namespace AiIncubator.Server.Common.Helpers.Configurations;

public class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    public string ServerUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public int VectorSize { get; init; } = 1024;
}
