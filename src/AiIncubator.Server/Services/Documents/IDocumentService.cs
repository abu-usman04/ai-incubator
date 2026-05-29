using AiIncubator.Server.Common;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Documents;

public interface IDocumentService
{
    Task<Document> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        string? moduleId,
        CancellationToken cancellationToken);

    Task<Document> GetAsync(string id, CancellationToken cancellationToken);

    Task<PagedResult<Document>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
