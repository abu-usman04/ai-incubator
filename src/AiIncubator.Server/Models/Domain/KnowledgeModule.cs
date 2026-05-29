using AiIncubator.Server.Common.Enums;

namespace AiIncubator.Server.Models.Domain;

public class KnowledgeModule
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; set; }

    public int DocumentCount { get; set; }

    public ModuleStatus Status { get; set; }

    public DateTime CreatedAt { get; init; }
}
