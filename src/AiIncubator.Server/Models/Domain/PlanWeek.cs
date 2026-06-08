namespace AiIncubator.Server.Models.Domain;

public class PlanWeek
{
    public required int WeekNumber { get; init; }

    public required string Theme { get; init; }

    public IReadOnlyList<PlanTask> Tasks { get; init; } = [];
}
