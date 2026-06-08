using AiIncubator.Server.Common.Enums;

namespace AiIncubator.Server.Models.Domain;

public class PlanTask
{
    public required string Id { get; init; }

    public required PlanTaskType Type { get; init; }

    public required string Title { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? ReferenceId { get; set; }

    public bool IsComplete { get; set; }

    public DateTime? DueAt { get; set; }
}
