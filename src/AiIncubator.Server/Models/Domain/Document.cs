using AiIncubator.Server.Common.Enums;

namespace AiIncubator.Server.Models.Domain;

public class Document
{
    public required string Id { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public string? ModuleId { get; set; }

    public DocumentStatus Status { get; set; }

    public int ChunkCount { get; set; }

    public long SizeBytes { get; init; }

    public DateTime CreatedAt { get; init; }
}
