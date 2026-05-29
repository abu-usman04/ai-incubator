using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Modules;

public interface IModuleService
{
    Task<KnowledgeModule> CreateAsync(string name, string? description, CancellationToken cancellationToken);

    Task<KnowledgeModule> GetAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeModule>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Re-aligns a module with its documents: counts indexed documents and marks the module ready.
    /// </summary>
    Task<KnowledgeModule> SyncAsync(string id, CancellationToken cancellationToken);
}
