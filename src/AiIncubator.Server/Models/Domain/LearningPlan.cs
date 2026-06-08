namespace AiIncubator.Server.Models.Domain;

public class LearningPlan
{
    public required string Id { get; init; }

    public required string Goal { get; init; }

    public string? Summary { get; init; }

    public string? UserId { get; init; }

    public IReadOnlyList<PlanWeek> Weeks { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}
