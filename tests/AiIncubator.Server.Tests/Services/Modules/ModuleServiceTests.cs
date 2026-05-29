namespace AiIncubator.Server.Tests.Services.Modules;

using FluentAssertions;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Documents;
using AiIncubator.Server.Services.Modules;

public class ModuleServiceTests
{
    private readonly InMemoryModuleRepository _modules = new();
    private readonly InMemoryDocumentRepository _documents = new();
    private readonly ModuleService _service;

    public ModuleServiceTests()
    {
        _service = new ModuleService(_modules, _documents);
    }

    [Fact]
    public async Task CreateAsync_BlankName_ThrowsBadRequest()
    {
        Func<Task> act = () => _service.CreateAsync("  ", null, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("BAD_REQUEST");
    }

    [Fact]
    public async Task SyncAsync_CountsIndexedDocumentsAndMarksReady()
    {
        KnowledgeModule module = await _service.CreateAsync("Onboarding", null, CancellationToken.None);
        await _documents.AddAsync(
            new Document
            {
                Id = "d1", FileName = "a.txt", ContentType = "text/plain",
                ModuleId = module.Id, Status = DocumentStatus.Indexed, CreatedAt = DateTime.UtcNow
            },
            CancellationToken.None);
        await _documents.AddAsync(
            new Document
            {
                Id = "d2", FileName = "b.txt", ContentType = "text/plain",
                ModuleId = module.Id, Status = DocumentStatus.Failed, CreatedAt = DateTime.UtcNow
            },
            CancellationToken.None);

        KnowledgeModule synced = await _service.SyncAsync(module.Id, CancellationToken.None);

        synced.DocumentCount.Should().Be(1);
        synced.Status.Should().Be(ModuleStatus.Ready);
    }

    [Fact]
    public async Task GetAsync_Missing_ThrowsNotFound()
    {
        Func<Task> act = () => _service.GetAsync("nope", CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("NOT_FOUND");
    }
}
