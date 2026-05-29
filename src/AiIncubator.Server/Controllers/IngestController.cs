using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Attributes;
using AiIncubator.Server.Models.RequestModels.Ingest;
using AiIncubator.Server.Services.Ingestion;

namespace AiIncubator.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/ingest/")]
[ControllerName(name: "ingest")]
public class IngestController(IIngestionService ingestionService, ILogger<IngestController> logger) : ControllerBase
{
    #region Public methods region

    /// <summary>Ingests a single text document into the RAG index.</summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST api/ingest
    ///     {
    ///       "documentId": "doc-1",
    ///       "text": "Some long passage of text...",
    ///       "metadata": { "source": "manual" }
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<IngestTextResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ApiResponse<IngestTextResponse>> Ingest(
        [FromBody] IngestTextRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Ingesting document {DocumentId}", request.DocumentId);

        int chunkCount = await ingestionService.IngestAsync(
            request.DocumentId,
            request.Text,
            request.Metadata ?? new Dictionary<string, string>(),
            cancellationToken);

        return ApiResponse<IngestTextResponse>.Ok(new IngestTextResponse(request.DocumentId, chunkCount));
    }

    #endregion
}
