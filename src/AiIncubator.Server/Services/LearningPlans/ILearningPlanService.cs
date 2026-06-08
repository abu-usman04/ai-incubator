using AiIncubator.Server.Common;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.LearningPlans;

namespace AiIncubator.Server.Services.LearningPlans;

public interface ILearningPlanService
{
    Task<LearningPlan> GeneratePlanAsync(GeneratePlanRequest request, string? userId, CancellationToken cancellationToken);

    Task<LearningPlan> GetAsync(string id, CancellationToken cancellationToken);

    Task<PagedResult<LearningPlan>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<LearningPlan> CompleteTaskAsync(string planId, string taskId, CancellationToken cancellationToken);
}
