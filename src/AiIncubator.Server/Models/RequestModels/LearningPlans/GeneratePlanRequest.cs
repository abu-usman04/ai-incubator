namespace AiIncubator.Server.Models.RequestModels.LearningPlans;

public class GeneratePlanRequest
{
    public required string Goal { get; init; }

    public int WeekCount { get; init; } = 4;

    public IReadOnlyList<string>? ModuleIds { get; init; }
}
