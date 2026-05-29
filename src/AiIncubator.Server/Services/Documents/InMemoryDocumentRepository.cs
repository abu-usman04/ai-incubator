using System.Collections.Concurrent;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Documents;

public class InMemoryDocumentRepository : IDocumentRepository
{
    #region Private fields region

    private readonly ConcurrentDictionary<string, Document> _documents = new();

    #endregion

    #region Public methods region

    public Task<Document> AddAsync(Document document, CancellationToken cancellationToken)
    {
        _documents[document.Id] = document;
        return Task.FromResult(document);
    }

    public Task UpdateAsync(Document document, CancellationToken cancellationToken)
    {
        _documents[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task<Document?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _documents.TryGetValue(id, out Document? document);
        return Task.FromResult(document);
    }

    public Task<IReadOnlyList<Document>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        IReadOnlyList<Document> items = _documents.Values
            .OrderByDescending(document => document.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_documents.Count);
    }

    public Task<IReadOnlyList<Document>> ListByModuleAsync(string moduleId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Document> items = _documents.Values
            .Where(document => document.ModuleId == moduleId)
            .OrderByDescending(document => document.CreatedAt)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_documents.TryRemove(id, out _));
    }

    #endregion
}
