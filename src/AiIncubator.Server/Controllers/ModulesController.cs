using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Attributes;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.Modules;
using AiIncubator.Server.Services.Modules;

namespace AiIncubator.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/modules/")]
[ControllerName(name: "modules")]
public class ModulesController(IModuleService moduleService) : ControllerBase
{
    #region Public methods region

    /// <summary>Creates a knowledge module that groups related documents.</summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST api/modules
    ///     {
    ///       "name": "Onboarding",
    ///       "description": "Company onboarding handbook"
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<KnowledgeModule>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<KnowledgeModule>> Create(
        [FromBody] CreateModuleRequest request,
        CancellationToken cancellationToken)
    {
        KnowledgeModule module = await moduleService.CreateAsync(request.Name, request.Description, cancellationToken);
        return ApiResponse<KnowledgeModule>.Ok(module);
    }

    /// <summary>Lists all knowledge modules, most recent first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<KnowledgeModule>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<IReadOnlyList<KnowledgeModule>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<KnowledgeModule> modules = await moduleService.ListAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<KnowledgeModule>>.Ok(modules);
    }

    /// <summary>Gets a single module by id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<KnowledgeModule>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<KnowledgeModule>> Get(string id, CancellationToken cancellationToken)
    {
        KnowledgeModule module = await moduleService.GetAsync(id, cancellationToken);
        return ApiResponse<KnowledgeModule>.Ok(module);
    }

    /// <summary>Syncs a module with its documents, refreshing its indexed-document count.</summary>
    [HttpPost("{id}/sync")]
    [ProducesResponseType(typeof(ApiResponse<KnowledgeModule>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<KnowledgeModule>> Sync(string id, CancellationToken cancellationToken)
    {
        KnowledgeModule module = await moduleService.SyncAsync(id, cancellationToken);
        return ApiResponse<KnowledgeModule>.Ok(module);
    }

    #endregion
}
