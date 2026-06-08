using System.Net;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.Glm;
using AiIncubator.Server.Models.RequestModels.LearningPlans;
using AiIncubator.Server.Services.Chat;
using AiIncubator.Server.Services.Documents;
using AiIncubator.Server.Services.Modules;

namespace AiIncubator.Server.Services.LearningPlans;

public class LearningPlanService(
    IChatCompletionClient chat,
    IModuleService modules,
    IDocumentRepository documents,
    ILearningPlanRepository repository,
    LearningPlanPromptBuilder promptBuilder) : ILearningPlanService
{
    #region Private fields region

    private const int CatalogDocumentLimit = 200;
    private const int DaysPerWeek = 7;

    #endregion

    #region Public methods region

    public async Task<LearningPlan> GeneratePlanAsync(
        GeneratePlanRequest request,
        string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Goal))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Goal is required.", "BAD_REQUEST");
        }

        IReadOnlyList<KnowledgeModule> moduleCatalog = await modules.ListAsync(cancellationToken);
        IReadOnlyList<Document> documentCatalog = await documents.ListAsync(1, CatalogDocumentLimit, cancellationToken);

        GlmChatRequest planRequest = promptBuilder.BuildPlanRequest(
            request.Goal.Trim(), request.WeekCount, moduleCatalog, documentCatalog);
        GlmChatResponse response = await chat.ChatAsync(planRequest, cancellationToken);

        GeneratedPlan? generated = GlmToolCallParser.Parse<GeneratedPlan>(response, "submit_learning_plan");
        if (generated is null || generated.Weeks.Count == 0)
        {
            throw new AppException(
                HttpStatusCode.InternalServerError, "Plan generation returned no weeks.", "INTERNAL_ERROR");
        }

        var knownIds = moduleCatalog.Select(module => module.Id)
            .Concat(documentCatalog.Select(document => document.Id))
            .ToHashSet();

        DateTime createdAt = DateTime.UtcNow;
        var plan = new LearningPlan
        {
            Id = Guid.NewGuid().ToString("N"),
            Goal = request.Goal.Trim(),
            Summary = generated.Summary,
            UserId = userId,
            CreatedAt = createdAt,
            Weeks = generated.Weeks
                .OrderBy(week => week.WeekNumber)
                .Select(week => MapWeek(week, knownIds, createdAt))
                .ToList()
        };

        return await repository.AddAsync(plan, cancellationToken);
    }

    public async Task<LearningPlan> GetAsync(string id, CancellationToken cancellationToken)
    {
        LearningPlan? plan = await repository.GetByIdAsync(id, cancellationToken);
        return plan ?? throw new AppException(HttpStatusCode.NotFound, $"Plan '{id}' not found.", "NOT_FOUND");
    }

    public async Task<PagedResult<LearningPlan>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        int safePage = page < 1 ? 1 : page;
        int safePageSize = Math.Clamp(pageSize, 1, 100);

        IReadOnlyList<LearningPlan> items = await repository.ListAsync(safePage, safePageSize, cancellationToken);
        int total = await repository.CountAsync(cancellationToken);

        return new PagedResult<LearningPlan>
        {
            Items = items,
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = total
        };
    }

    public async Task<LearningPlan> CompleteTaskAsync(string planId, string taskId, CancellationToken cancellationToken)
    {
        LearningPlan plan = await GetAsync(planId, cancellationToken);

        PlanTask? task = plan.Weeks.SelectMany(week => week.Tasks).FirstOrDefault(item => item.Id == taskId);
        if (task is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Task '{taskId}' not found.", "NOT_FOUND");
        }

        task.IsComplete = true;
        await repository.UpdateAsync(plan, cancellationToken);
        return plan;
    }

    #endregion

    #region Private methods region

    private static PlanWeek MapWeek(GeneratedWeek week, IReadOnlySet<string> knownIds, DateTime createdAt) => new()
    {
        WeekNumber = week.WeekNumber,
        Theme = week.Theme,
        Tasks = week.Tasks.Select(task => MapTask(task, week.WeekNumber, knownIds, createdAt)).ToList()
    };

    private static PlanTask MapTask(
        GeneratedTask task,
        int weekNumber,
        IReadOnlySet<string> knownIds,
        DateTime createdAt) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = ParseType(task.Type),
        Title = task.Title,
        Description = task.Description,
        ReferenceId = task.ReferenceId is not null && knownIds.Contains(task.ReferenceId) ? task.ReferenceId : null,
        IsComplete = false,
        DueAt = createdAt.AddDays(Math.Max(0, weekNumber - 1) * DaysPerWeek)
    };

    private static PlanTaskType ParseType(string type) => type.ToLowerInvariant() switch
    {
        "quiz" => PlanTaskType.Quiz,
        "chat" => PlanTaskType.Chat,
        _ => PlanTaskType.Read
    };

    #endregion
}
