using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Attributes;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.LearningPlans;
using AiIncubator.Server.Services.LearningPlans;

namespace AiIncubator.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/learning-plans/")]
[ControllerName(name: "learning-plans")]
public class LearningPlanController(ILearningPlanService planService) : ControllerBase
{
    #region Public methods region

    /// <summary>Generates a multi-week learning plan grounded in the knowledge base.</summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST api/learning-plans
    ///     {
    ///       "goal": "Become productive with the onboarding material",
    ///       "weekCount": 4
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LearningPlan>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<LearningPlan>> Generate(
        [FromBody] GeneratePlanRequest request,
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        LearningPlan plan = await planService.GeneratePlanAsync(request, userId, cancellationToken);
        return ApiResponse<LearningPlan>.Ok(plan);
    }

    /// <summary>Lists generated learning plans, most recent first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LearningPlan>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PagedResult<LearningPlan>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<LearningPlan> result = await planService.ListAsync(page, pageSize, cancellationToken);
        return ApiResponse<PagedResult<LearningPlan>>.Ok(result);
    }

    /// <summary>Gets a learning plan with its weeks and tasks.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LearningPlan>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<LearningPlan>> Get(string id, CancellationToken cancellationToken)
    {
        LearningPlan plan = await planService.GetAsync(id, cancellationToken);
        return ApiResponse<LearningPlan>.Ok(plan);
    }

    /// <summary>Marks a plan task as complete.</summary>
    [HttpPost("{planId}/tasks/{taskId}/complete")]
    [ProducesResponseType(typeof(ApiResponse<LearningPlan>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<LearningPlan>> CompleteTask(
        string planId,
        string taskId,
        CancellationToken cancellationToken)
    {
        LearningPlan plan = await planService.CompleteTaskAsync(planId, taskId, cancellationToken);
        return ApiResponse<LearningPlan>.Ok(plan);
    }

    #endregion
}
