using System.Net;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Documents;

namespace AiIncubator.Server.Services.Modules;

public class ModuleService(
    IModuleRepository repository,
    IDocumentRepository documentRepository) : IModuleService
{
    #region Public methods region

    public async Task<KnowledgeModule> CreateAsync(string name, string? description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Module name is required.", "BAD_REQUEST");
        }

        var module = new KnowledgeModule
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
            Description = description,
            DocumentCount = 0,
            Status = ModuleStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        return await repository.AddAsync(module, cancellationToken);
    }

    public async Task<KnowledgeModule> GetAsync(string id, CancellationToken cancellationToken)
    {
        KnowledgeModule? module = await repository.GetByIdAsync(id, cancellationToken);
        if (module is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Module '{id}' not found.", "NOT_FOUND");
        }

        return module;
    }

    public Task<IReadOnlyList<KnowledgeModule>> ListAsync(CancellationToken cancellationToken)
    {
        return repository.ListAsync(cancellationToken);
    }

    public async Task<KnowledgeModule> SyncAsync(string id, CancellationToken cancellationToken)
    {
        KnowledgeModule module = await GetAsync(id, cancellationToken);

        IReadOnlyList<Document> documents = await documentRepository.ListByModuleAsync(id, cancellationToken);
        module.DocumentCount = documents.Count(document => document.Status == DocumentStatus.Indexed);
        module.Status = ModuleStatus.Ready;
        await repository.UpdateAsync(module, cancellationToken);

        return module;
    }

    #endregion
}
