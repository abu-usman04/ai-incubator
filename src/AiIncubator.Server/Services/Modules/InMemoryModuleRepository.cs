using System.Collections.Concurrent;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Modules;

public class InMemoryModuleRepository : IModuleRepository
{
    #region Private fields region

    private readonly ConcurrentDictionary<string, KnowledgeModule> _modules = new();

    #endregion

    #region Public methods region

    public Task<KnowledgeModule> AddAsync(KnowledgeModule module, CancellationToken cancellationToken)
    {
        _modules[module.Id] = module;
        return Task.FromResult(module);
    }

    public Task UpdateAsync(KnowledgeModule module, CancellationToken cancellationToken)
    {
        _modules[module.Id] = module;
        return Task.CompletedTask;
    }

    public Task<KnowledgeModule?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _modules.TryGetValue(id, out KnowledgeModule? module);
        return Task.FromResult(module);
    }

    public Task<IReadOnlyList<KnowledgeModule>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<KnowledgeModule> items = _modules.Values
            .OrderByDescending(module => module.CreatedAt)
            .ToList();

        return Task.FromResult(items);
    }

    #endregion
}
