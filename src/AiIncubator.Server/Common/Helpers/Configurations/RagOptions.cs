namespace AiIncubator.Server.Common.Helpers.Configurations;

public class RagOptions
{
    public const string SectionName = "Rag";

    public int ChunkSize { get; init; } = 800;

    public int ChunkOverlap { get; init; } = 100;

    public int DefaultTopK { get; init; } = 4;
}
