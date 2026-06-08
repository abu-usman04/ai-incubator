namespace AiIncubator.Server.Models.Domain;

/// <summary>A plan task whose due date has passed, paired with its owning plan for reminders.</summary>
public class DuePlanTask
{
    public required string PlanId { get; init; }

    public string? UserId { get; init; }

    public required PlanTask Task { get; init; }
}
