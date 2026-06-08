namespace AiIncubator.Server.Tests.Services.LearningPlans;

using FluentAssertions;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.LearningPlans;

public class InMemoryLearningPlanRepositoryTests
{
    private readonly InMemoryLearningPlanRepository _repository = new();
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListDueTasksAsync_ReturnsOnlyIncompletePastDueTasks()
    {
        await _repository.AddAsync(BuildPlan(), CancellationToken.None);

        IReadOnlyList<DuePlanTask> due = await _repository.ListDueTasksAsync(Now, CancellationToken.None);

        due.Should().ContainSingle();
        due[0].Task.Id.Should().Be("due-incomplete");
        due[0].UserId.Should().Be("dev-user");
    }

    private static LearningPlan BuildPlan() => new()
    {
        Id = "plan-1",
        Goal = "g",
        UserId = "dev-user",
        CreatedAt = Now.AddDays(-7),
        Weeks =
        [
            new PlanWeek
            {
                WeekNumber = 1,
                Theme = "T",
                Tasks =
                [
                    new PlanTask
                    {
                        Id = "due-incomplete", Type = PlanTaskType.Read, Title = "A",
                        IsComplete = false, DueAt = Now.AddDays(-1)
                    },
                    new PlanTask
                    {
                        Id = "due-complete", Type = PlanTaskType.Read, Title = "B",
                        IsComplete = true, DueAt = Now.AddDays(-1)
                    },
                    new PlanTask
                    {
                        Id = "future", Type = PlanTaskType.Read, Title = "C",
                        IsComplete = false, DueAt = Now.AddDays(1)
                    },
                    new PlanTask
                    {
                        Id = "no-due", Type = PlanTaskType.Read, Title = "D",
                        IsComplete = false, DueAt = null
                    }
                ]
            }
        ]
    };
}
