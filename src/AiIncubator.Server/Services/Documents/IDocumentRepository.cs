using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Documents;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken);

    Task UpdateAsync(Document document, CancellationToken cancellationToken);

    Task<Document?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Document>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Document>> ListByModuleAsync(string moduleId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
