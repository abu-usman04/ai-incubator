using System.Net;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Ingestion;
using AiIncubator.Server.Services.VectorStore;

namespace AiIncubator.Server.Services.Documents;

public class DocumentService(
    IDocumentRepository repository,
    IDocumentTextExtractor extractor,
    IIngestionService ingestionService,
    IVectorStore vectorStore,
    IOptions<DocumentsOptions> documentsOptions,
    ILogger<DocumentService> logger) : IDocumentService
{
    #region Private fields region

    private const string SourceDocumentIdKey = "source_document_id";
    private const string FileNameKey = "file_name";
    private const string ContentTypeKey = "content_type";
    private const string ModuleIdKey = "module_id";
    private readonly DocumentsOptions _options = documentsOptions.Value;

    #endregion

    #region Public methods region

    public async Task<Document> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        string? moduleId,
        CancellationToken cancellationToken)
    {
        ValidateUpload(fileName, sizeBytes);

        var document = new Document
        {
            Id = Guid.NewGuid().ToString("N"),
            FileName = fileName,
            ContentType = contentType,
            ModuleId = moduleId,
            Status = DocumentStatus.Processing,
            SizeBytes = sizeBytes,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(document, cancellationToken);

        try
        {
            string text = await extractor.ExtractAsync(content, fileName, cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new AppException(
                    HttpStatusCode.BadRequest,
                    "No extractable text found in the uploaded file.",
                    "BAD_REQUEST");
            }

            int chunkCount = await ingestionService.IngestAsync(
                document.Id,
                text,
                BuildMetadata(document),
                cancellationToken);

            document.ChunkCount = chunkCount;
            document.Status = DocumentStatus.Indexed;
            await repository.UpdateAsync(document, cancellationToken);
            return document;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {DocumentId}", document.Id);
            document.Status = DocumentStatus.Failed;
            await repository.UpdateAsync(document, cancellationToken);
            throw;
        }
    }

    public async Task<Document> GetAsync(string id, CancellationToken cancellationToken)
    {
        Document? document = await repository.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Document '{id}' not found.", "NOT_FOUND");
        }

        return document;
    }

    public async Task<PagedResult<Document>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        int safePage = page < 1 ? 1 : page;
        int safePageSize = Math.Clamp(pageSize, 1, 100);

        IReadOnlyList<Document> items = await repository.ListAsync(safePage, safePageSize, cancellationToken);
        int total = await repository.CountAsync(cancellationToken);

        return new PagedResult<Document>
        {
            Items = items,
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = total
        };
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        Document document = await GetAsync(id, cancellationToken);
        await vectorStore.DeleteByDocumentAsync(document.Id, cancellationToken);
        await repository.DeleteAsync(document.Id, cancellationToken);
    }

    #endregion

    #region Private methods region

    private void ValidateUpload(string fileName, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new AppException(HttpStatusCode.BadRequest, "A file is required.", "BAD_REQUEST");
        }

        if (sizeBytes <= 0)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Uploaded file is empty.", "BAD_REQUEST");
        }

        if (sizeBytes > _options.MaxUploadBytes)
        {
            throw new AppException(
                HttpStatusCode.BadRequest,
                $"File exceeds the maximum size of {_options.MaxUploadBytes} bytes.",
                "BAD_REQUEST");
        }

        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new AppException(
                HttpStatusCode.BadRequest,
                $"Unsupported file type '{extension}'. Allowed: {string.Join(", ", _options.AllowedExtensions)}.",
                "BAD_REQUEST");
        }
    }

    private static Dictionary<string, string> BuildMetadata(Document document)
    {
        var metadata = new Dictionary<string, string>
        {
            [SourceDocumentIdKey] = document.Id,
            [FileNameKey] = document.FileName,
            [ContentTypeKey] = document.ContentType
        };

        if (!string.IsNullOrWhiteSpace(document.ModuleId))
        {
            metadata[ModuleIdKey] = document.ModuleId;
        }

        return metadata;
    }

    #endregion
}
