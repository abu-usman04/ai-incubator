using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Attributes;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Documents;

namespace AiIncubator.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/documents/")]
[ControllerName(name: "documents")]
public class DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    : ControllerBase
{
    #region Public methods region

    /// <summary>Uploads a file (.txt, .md, .pdf), extracts its text and indexes it for retrieval.</summary>
    /// <remarks>
    /// Sample request (multipart/form-data):
    ///
    ///     POST api/documents
    ///     file: &lt;binary&gt;
    ///     moduleId: optional-module-id
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Document>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ApiResponse<Document>> Upload(
        IFormFile file,
        [FromForm] string? moduleId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Uploading document {FileName}", file?.FileName);

        await using Stream stream = file!.OpenReadStream();
        Document document = await documentService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            moduleId,
            cancellationToken);

        return ApiResponse<Document>.Ok(document);
    }

    /// <summary>Lists indexed documents, most recent first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Document>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PagedResult<Document>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<Document> result = await documentService.ListAsync(page, pageSize, cancellationToken);
        return ApiResponse<PagedResult<Document>>.Ok(result);
    }

    /// <summary>Gets a single document by id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Document>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<Document>> Get(string id, CancellationToken cancellationToken)
    {
        Document document = await documentService.GetAsync(id, cancellationToken);
        return ApiResponse<Document>.Ok(document);
    }

    /// <summary>Deletes a document and its vectors.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<object>> Delete(string id, CancellationToken cancellationToken)
    {
        await documentService.DeleteAsync(id, cancellationToken);
        return ApiResponse<object>.Ok(new { id });
    }

    #endregion
}
