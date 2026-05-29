using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Modules;

public interface IModuleRepository
{
    Task<KnowledgeModule> AddAsync(KnowledgeModule module, CancellationToken cancellationToken);

    Task UpdateAsync(KnowledgeModule module, CancellationToken cancellationToken);

    Task<KnowledgeModule?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeModule>> ListAsync(CancellationToken cancellationToken);
}
