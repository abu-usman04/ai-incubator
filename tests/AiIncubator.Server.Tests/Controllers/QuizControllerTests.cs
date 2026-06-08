namespace AiIncubator.Server.Tests.Controllers;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Moq;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.Quiz;
using AiIncubator.Server.Tests.TestHelpers;

public class QuizControllerTests : IClassFixture<RagWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RagWebApplicationFactory _factory;

    public QuizControllerTests(RagWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Generate_ValidBody_Returns200WithQuiz()
    {
        _factory.Quiz.Reset();
        var quiz = new Quiz
        {
            Id = "quiz-1",
            Title = "Onboarding",
            Topic = "onboarding",
            Status = QuizStatus.Ready,
            Questions = [],
            CreatedAt = DateTime.UtcNow
        };
        _factory.Quiz
            .Setup(s => s.GenerateAsync(It.IsAny<GenerateQuizRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(quiz);

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/quizzes", new GenerateQuizRequest { Topic = "onboarding" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiResponse<Quiz>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<Quiz>>(JsonOptions);
        payload!.Success.Should().BeTrue();
        payload.Data!.Id.Should().Be("quiz-1");
    }

    [Fact]
    public async Task SubmitAnswer_Returns200WithUpdatedAttempt()
    {
        _factory.Quiz.Reset();
        var attempt = new QuizAttempt
        {
            Id = "attempt-1",
            QuizId = "quiz-1",
            Status = AttemptStatus.InProgress,
            MaxScore = 2,
            Score = 1,
            StartedAt = DateTime.UtcNow
        };
        _factory.Quiz
            .Setup(s => s.SubmitAnswerAsync("attempt-1", It.IsAny<SubmitAnswerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/quizzes/attempts/attempt-1/answers",
            new SubmitAnswerRequest { QuestionId = "q1", SelectedOptionIndex = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiResponse<QuizAttempt>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<QuizAttempt>>(JsonOptions);
        payload!.Data!.Score.Should().Be(1);
    }

    [Fact]
    public async Task Get_UnknownQuiz_Returns404()
    {
        _factory.Quiz.Reset();
        _factory.Quiz
            .Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AppException(HttpStatusCode.NotFound, "Quiz 'x' not found.", "NOT_FOUND"));

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("api/quizzes/x");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ApiResponse<object>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        payload!.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task Generate_ServiceBadRequest_Returns400()
    {
        _factory.Quiz.Reset();
        _factory.Quiz
            .Setup(s => s.GenerateAsync(It.IsAny<GenerateQuizRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AppException(HttpStatusCode.BadRequest, "topic or moduleId is required.", "BAD_REQUEST"));

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("api/quizzes", new GenerateQuizRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ApiResponse<object>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        payload!.Error!.Code.Should().Be("BAD_REQUEST");
    }
}
