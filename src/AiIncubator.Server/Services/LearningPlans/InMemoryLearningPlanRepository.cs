using System.Collections.Concurrent;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.LearningPlans;

public class InMemoryLearningPlanRepository : ILearningPlanRepository
{
    #region Private fields region

    private readonly ConcurrentDictionary<string, LearningPlan> _plans = new();

    #endregion

    #region Public methods region

    public Task<LearningPlan> AddAsync(LearningPlan plan, CancellationToken cancellationToken)
    {
        _plans[plan.Id] = plan;
        return Task.FromResult(plan);
    }

    public Task<LearningPlan?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _plans.TryGetValue(id, out LearningPlan? plan);
        return Task.FromResult(plan);
    }

    public Task UpdateAsync(LearningPlan plan, CancellationToken cancellationToken)
    {
        _plans[plan.Id] = plan;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LearningPlan>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        IReadOnlyList<LearningPlan> items = _plans.Values
            .OrderByDescending(plan => plan.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_plans.Count);
    }

    public Task<IReadOnlyList<DuePlanTask>> ListDueTasksAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        IReadOnlyList<DuePlanTask> due = _plans.Values
            .SelectMany(plan => plan.Weeks
                .SelectMany(week => week.Tasks)
                .Where(task => !task.IsComplete && task.DueAt is not null && task.DueAt <= nowUtc)
                .Select(task => new DuePlanTask { PlanId = plan.Id, UserId = plan.UserId, Task = task }))
            .ToList();

        return Task.FromResult(due);
    }

    #endregion
}
