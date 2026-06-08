namespace AiIncubator.Server.Tests.Controllers;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Moq;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.LearningPlans;
using AiIncubator.Server.Tests.TestHelpers;

public class LearningPlanControllerTests : IClassFixture<RagWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RagWebApplicationFactory _factory;

    public LearningPlanControllerTests(RagWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Generate_ValidBody_Returns200WithPlan()
    {
        _factory.Plans.Reset();
        var plan = new LearningPlan
        {
            Id = "plan-1",
            Goal = "Learn onboarding",
            UserId = "dev-user",
            Weeks = [],
            CreatedAt = DateTime.UtcNow
        };
        _factory.Plans
            .Setup(s => s.GeneratePlanAsync(It.IsAny<GeneratePlanRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/learning-plans", new GeneratePlanRequest { Goal = "Learn onboarding" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiResponse<LearningPlan>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<LearningPlan>>(JsonOptions);
        payload!.Data!.Id.Should().Be("plan-1");
    }

    [Fact]
    public async Task CompleteTask_UnknownTask_Returns404()
    {
        _factory.Plans.Reset();
        _factory.Plans
            .Setup(s => s.CompleteTaskAsync("plan-1", "missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AppException(HttpStatusCode.NotFound, "Task 'missing' not found.", "NOT_FOUND"));

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsync("api/learning-plans/plan-1/tasks/missing/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ApiResponse<object>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        payload!.Error!.Code.Should().Be("NOT_FOUND");
    }
}
