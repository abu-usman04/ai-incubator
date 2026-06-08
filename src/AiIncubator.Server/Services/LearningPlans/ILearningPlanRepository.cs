using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.LearningPlans;

public interface ILearningPlanRepository
{
    Task<LearningPlan> AddAsync(LearningPlan plan, CancellationToken cancellationToken);

    Task<LearningPlan?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task UpdateAsync(LearningPlan plan, CancellationToken cancellationToken);

    Task<IReadOnlyList<LearningPlan>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DuePlanTask>> ListDueTasksAsync(DateTime nowUtc, CancellationToken cancellationToken);
}
