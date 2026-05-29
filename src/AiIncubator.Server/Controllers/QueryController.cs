using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Attributes;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.Query;
using AiIncubator.Server.Services.Retrieval;

namespace AiIncubator.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/query/")]
[ControllerName(name: "query")]
public class QueryController(IQueryService queryService, ILogger<QueryController> logger) : ControllerBase
{
    #region Public methods region

    /// <summary>
    /// Answers a question using retrieval-augmented generation.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST api/query
    ///     {
    ///       "question": "What is RAG?",
    ///       "topK": 4
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RagAnswer>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ApiResponse<RagAnswer>> Query(
        [FromBody] QueryRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Answering question (topK={TopK})", request.TopK);

        RagAnswer answer = await queryService.AnswerAsync(request.Question, request.TopK, cancellationToken);
        return ApiResponse<RagAnswer>.Ok(answer);
    }

    #endregion
}