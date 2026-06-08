namespace AiIncubator.Server.Services.LearningPlans;

/// <summary>Shape of the arguments returned by the GLM <c>submit_learning_plan</c> tool call.</summary>
internal sealed class GeneratedPlan
{
    public string? Summary { get; init; }

    public List<GeneratedWeek> Weeks { get; init; } = [];
}
