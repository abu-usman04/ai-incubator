namespace AiIncubator.Server.Tests.Services.LearningPlans;

using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.Glm;
using AiIncubator.Server.Models.RequestModels.LearningPlans;
using AiIncubator.Server.Services.Chat;
using AiIncubator.Server.Services.Documents;
using AiIncubator.Server.Services.LearningPlans;
using AiIncubator.Server.Services.Modules;

public class LearningPlanServiceTests
{
    private readonly Mock<IChatCompletionClient> _chat = new();
    private readonly Mock<IModuleService> _modules = new();
    private readonly Mock<IDocumentRepository> _documents = new();
    private readonly InMemoryLearningPlanRepository _repository = new();
    private readonly LearningPlanService _service;

    public LearningPlanServiceTests()
    {
        var promptBuilder = new LearningPlanPromptBuilder(Options.Create(new ChatOptions { Model = "glm-5.1" }));
        _service = new LearningPlanService(
            _chat.Object, _modules.Object, _documents.Object, _repository, promptBuilder);
        _modules.Setup(m => m.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<KnowledgeModule>());
        _documents.Setup(d => d.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Document>());
    }

    [Fact]
    public async Task GeneratePlanAsync_ParsesToolCall_IntoTypedPlan()
    {
        StubModulesWith("mod-1");
        const string args = """
        {
          "summary": "A focused plan.",
          "weeks": [
            { "weekNumber": 1, "theme": "Basics",
              "tasks": [ { "type": "read", "title": "Read handbook", "referenceId": "mod-1" } ] },
            { "weekNumber": 2, "theme": "Practice",
              "tasks": [ { "type": "quiz", "title": "Quiz yourself" } ] }
          ]
        }
        """;
        StubPlan(args);

        LearningPlan plan = await _service.GeneratePlanAsync(
            new GeneratePlanRequest { Goal = "Learn onboarding", WeekCount = 2 }, "dev-user", CancellationToken.None);

        plan.Weeks.Should().HaveCount(2);
        plan.UserId.Should().Be("dev-user");
        plan.Weeks[0].Tasks[0].Type.Should().Be(PlanTaskType.Read);
        plan.Weeks[0].Tasks[0].ReferenceId.Should().Be("mod-1");
        plan.Weeks[1].Tasks[0].Type.Should().Be(PlanTaskType.Quiz);
        (await _repository.GetByIdAsync(plan.Id, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task GeneratePlanAsync_NullsUnknownReferenceId()
    {
        StubModulesWith("mod-1");
        const string args = """
        { "weeks": [ { "weekNumber": 1, "theme": "T",
          "tasks": [ { "type": "read", "title": "X", "referenceId": "ghost" } ] } ] }
        """;
        StubPlan(args);

        LearningPlan plan = await _service.GeneratePlanAsync(
            new GeneratePlanRequest { Goal = "g" }, null, CancellationToken.None);

        plan.Weeks[0].Tasks[0].ReferenceId.Should().BeNull();
    }

    [Fact]
    public async Task GeneratePlanAsync_BlankGoal_ThrowsBadRequest()
    {
        Func<Task> act = () => _service.GeneratePlanAsync(
            new GeneratePlanRequest { Goal = "  " }, null, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("BAD_REQUEST");
    }

    [Fact]
    public async Task GeneratePlanAsync_NoToolCall_ThrowsInternalError()
    {
        _chat.Setup(c => c.ChatAsync(It.IsAny<GlmChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GlmChatResponse { Choices = [new GlmChoice { Message = new GlmMessage { Content = "x" } }] });

        Func<Task> act = () => _service.GeneratePlanAsync(
            new GeneratePlanRequest { Goal = "g" }, null, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("INTERNAL_ERROR");
    }

    [Fact]
    public async Task CompleteTaskAsync_FlipsIsComplete()
    {
        StubModulesWith("mod-1");
        StubPlan("""
        { "weeks": [ { "weekNumber": 1, "theme": "T",
          "tasks": [ { "type": "read", "title": "X" } ] } ] }
        """);
        LearningPlan plan = await _service.GeneratePlanAsync(
            new GeneratePlanRequest { Goal = "g" }, null, CancellationToken.None);
        string taskId = plan.Weeks[0].Tasks[0].Id;

        LearningPlan updated = await _service.CompleteTaskAsync(plan.Id, taskId, CancellationToken.None);

        updated.Weeks[0].Tasks[0].IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteTaskAsync_UnknownTask_ThrowsNotFound()
    {
        StubPlan("""{ "weeks": [ { "weekNumber": 1, "theme": "T", "tasks": [ { "type": "read", "title": "X" } ] } ] }""");
        LearningPlan plan = await _service.GeneratePlanAsync(
            new GeneratePlanRequest { Goal = "g" }, null, CancellationToken.None);

        Func<Task> act = () => _service.CompleteTaskAsync(plan.Id, "missing", CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("NOT_FOUND");
    }

    private void StubModulesWith(string moduleId)
    {
        _modules.Setup(m => m.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new KnowledgeModule { Id = moduleId, Name = "Onboarding", CreatedAt = DateTime.UtcNow }
            });
    }

    private void StubPlan(string argumentsJson)
    {
        _chat.Setup(c => c.ChatAsync(It.IsAny<GlmChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GlmChatResponse
            {
                Choices =
                [
                    new GlmChoice
                    {
                        Message = new GlmMessage
                        {
                            ToolCalls =
                            [
                                new GlmToolCall
                                {
                                    Function = new GlmFunctionCall
                                    {
                                        Name = "submit_learning_plan",
                                        Arguments = argumentsJson
                                    }
                                }
                            ]
                        }
                    }
                ]
            });
    }
}
