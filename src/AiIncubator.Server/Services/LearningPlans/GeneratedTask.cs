namespace AiIncubator.Server.Services.LearningPlans;

/// <summary>Shape of a single task returned by the GLM <c>submit_learning_plan</c> tool call.</summary>
internal sealed class GeneratedTask
{
    public string Type { get; init; } = "read";

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ReferenceId { get; init; }
}
