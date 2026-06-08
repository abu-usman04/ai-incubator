namespace AiIncubator.Server.Services.LearningPlans;

/// <summary>Shape of a single week returned by the GLM <c>submit_learning_plan</c> tool call.</summary>
internal sealed class GeneratedWeek
{
    public int WeekNumber { get; init; }

    public string Theme { get; init; } = string.Empty;

    public List<GeneratedTask> Tasks { get; init; } = [];
}
