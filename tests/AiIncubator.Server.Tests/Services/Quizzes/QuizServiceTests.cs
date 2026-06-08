namespace AiIncubator.Server.Tests.Services.Quizzes;

using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.Glm;
using AiIncubator.Server.Models.RequestModels.Quiz;
using AiIncubator.Server.Services.Chat;
using AiIncubator.Server.Services.Modules;
using AiIncubator.Server.Services.Quizzes;
using AiIncubator.Server.Services.Retrieval;

public class QuizServiceTests
{
    private readonly Mock<IQueryService> _query = new();
    private readonly Mock<IChatCompletionClient> _chat = new();
    private readonly Mock<IModuleService> _modules = new();
    private readonly InMemoryQuizRepository _quizzes = new();
    private readonly InMemoryQuizAttemptRepository _attempts = new();
    private readonly QuizService _service;

    public QuizServiceTests()
    {
        IOptions<RagOptions> options = Options.Create(new RagOptions { DefaultTopK = 4 });
        var promptBuilder = new QuizPromptBuilder(Options.Create(new ChatOptions { Model = "glm-5.1" }));
        _service = new QuizService(
            _query.Object,
            _chat.Object,
            _quizzes,
            _attempts,
            _modules.Object,
            promptBuilder,
            options);
    }

    #region Generation

    [Fact]
    public async Task GenerateAsync_ParsesToolCall_IntoTypedQuiz()
    {
        StubRetrieval();
        const string args = """
        {
          "questions": [
            { "type": "multiple_choice", "prompt": "Capital of France?",
              "options": ["Paris", "Rome"], "correctOptionIndex": 0 },
            { "type": "short_answer", "prompt": "Define RAG.", "expectedAnswer": "Retrieval augmented generation" }
          ]
        }
        """;
        StubGeneration(args);

        Quiz quiz = await _service.GenerateAsync(
            new GenerateQuizRequest { Topic = "geography", QuestionCount = 2 }, CancellationToken.None);

        quiz.Questions.Should().HaveCount(2);
        quiz.Questions[0].Type.Should().Be(QuestionType.MultipleChoice);
        quiz.Questions[0].Options.Should().Equal("Paris", "Rome");
        quiz.Questions[0].CorrectOptionIndex.Should().Be(0);
        quiz.Questions[1].Type.Should().Be(QuestionType.ShortAnswer);
        quiz.Questions[1].ExpectedAnswer.Should().Be("Retrieval augmented generation");

        (await _quizzes.GetByIdAsync(quiz.Id, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAsync_NoRetrievedChunks_ThrowsBadRequest()
    {
        _query.Setup(q => q.RetrieveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RetrievedChunk>());

        Func<Task> act = () => _service.GenerateAsync(
            new GenerateQuizRequest { Topic = "empty" }, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("BAD_REQUEST");
    }

    [Fact]
    public async Task GenerateAsync_NoToolCall_ThrowsInternalError()
    {
        StubRetrieval();
        _chat.Setup(c => c.ChatAsync(It.IsAny<GlmChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GlmChatResponse { Choices = [new GlmChoice { Message = new GlmMessage { Content = "no tools" } }] });

        Func<Task> act = () => _service.GenerateAsync(
            new GenerateQuizRequest { Topic = "geography" }, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("INTERNAL_ERROR");
    }

    [Fact]
    public async Task GenerateAsync_NoTopicOrModule_ThrowsBadRequest()
    {
        Func<Task> act = () => _service.GenerateAsync(new GenerateQuizRequest(), CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("BAD_REQUEST");
    }

    #endregion

    #region Attempts

    [Fact]
    public async Task StartAttemptAsync_SetsMaxScoreAndInProgress()
    {
        Quiz quiz = await SeedQuiz();

        QuizAttempt attempt = await _service.StartAttemptAsync(quiz.Id, CancellationToken.None);

        attempt.Status.Should().Be(AttemptStatus.InProgress);
        attempt.MaxScore.Should().Be(2);
        attempt.Score.Should().Be(0);
    }

    [Fact]
    public async Task StartAttemptAsync_UnknownQuiz_ThrowsNotFound()
    {
        Func<Task> act = () => _service.StartAttemptAsync("missing", CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task SubmitAnswerAsync_MultipleChoiceCorrect_AwardsPoints_NoLlmCall()
    {
        Quiz quiz = await SeedQuiz();
        QuizAttempt attempt = await _service.StartAttemptAsync(quiz.Id, CancellationToken.None);

        QuizAttempt updated = await _service.SubmitAnswerAsync(
            attempt.Id,
            new SubmitAnswerRequest { QuestionId = "q-mc", SelectedOptionIndex = 0 },
            CancellationToken.None);

        updated.Score.Should().Be(1);
        updated.Answers.Should().ContainSingle().Which.IsCorrect.Should().BeTrue();
        _chat.Verify(c => c.ChatAsync(It.IsAny<GlmChatRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAnswerAsync_MultipleChoiceWrong_AwardsZero()
    {
        Quiz quiz = await SeedQuiz();
        QuizAttempt attempt = await _service.StartAttemptAsync(quiz.Id, CancellationToken.None);

        QuizAttempt updated = await _service.SubmitAnswerAsync(
            attempt.Id,
            new SubmitAnswerRequest { QuestionId = "q-mc", SelectedOptionIndex = 1 },
            CancellationToken.None);

        updated.Score.Should().Be(0);
        updated.Answers.Single().IsCorrect.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitAnswerAsync_ShortAnswer_UsesGrader_AndCompletesAttempt()
    {
        Quiz quiz = await SeedQuiz();
        QuizAttempt attempt = await _service.StartAttemptAsync(quiz.Id, CancellationToken.None);
        await _service.SubmitAnswerAsync(
            attempt.Id, new SubmitAnswerRequest { QuestionId = "q-mc", SelectedOptionIndex = 0 }, CancellationToken.None);

        StubGrading(isCorrect: true, points: 1, feedback: "Good");

        QuizAttempt updated = await _service.SubmitAnswerAsync(
            attempt.Id,
            new SubmitAnswerRequest { QuestionId = "q-sa", Text = "retrieval augmented generation" },
            CancellationToken.None);

        updated.Status.Should().Be(AttemptStatus.Completed);
        updated.CompletedAt.Should().NotBeNull();
        updated.Score.Should().Be(2);
        updated.Answers.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubmitAnswerAsync_UnknownAttempt_ThrowsNotFound()
    {
        Func<Task> act = () => _service.SubmitAnswerAsync(
            "missing", new SubmitAnswerRequest { QuestionId = "q-mc" }, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task SubmitAnswerAsync_UnknownQuestion_ThrowsBadRequest()
    {
        Quiz quiz = await SeedQuiz();
        QuizAttempt attempt = await _service.StartAttemptAsync(quiz.Id, CancellationToken.None);

        Func<Task> act = () => _service.SubmitAnswerAsync(
            attempt.Id, new SubmitAnswerRequest { QuestionId = "nope" }, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("BAD_REQUEST");
    }

    #endregion

    #region Helpers

    private void StubRetrieval()
    {
        _query.Setup(q => q.RetrieveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new RetrievedChunk("doc#0", "Paris is the capital of France.", 0.9f, new Dictionary<string, string>())
            });
    }

    private void StubGeneration(string argumentsJson)
    {
        _chat.Setup(c => c.ChatAsync(It.IsAny<GlmChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResponse("submit_quiz", argumentsJson));
    }

    private void StubGrading(bool isCorrect, int points, string feedback)
    {
        string args = $$"""{ "isCorrect": {{(isCorrect ? "true" : "false")}}, "points": {{points}}, "feedback": "{{feedback}}" }""";
        _chat.Setup(c => c.ChatAsync(It.IsAny<GlmChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResponse("grade_answer", args));
    }

    private static GlmChatResponse ToolResponse(string functionName, string argumentsJson) => new()
    {
        Choices =
        [
            new GlmChoice
            {
                Message = new GlmMessage
                {
                    ToolCalls =
                    [
                        new GlmToolCall { Function = new GlmFunctionCall { Name = functionName, Arguments = argumentsJson } }
                    ]
                }
            }
        ]
    };

    private async Task<Quiz> SeedQuiz()
    {
        var quiz = new Quiz
        {
            Id = "quiz-1",
            Title = "Geography",
            Topic = "geography",
            Status = QuizStatus.Ready,
            CreatedAt = DateTime.UtcNow,
            Questions =
            [
                new QuizQuestion
                {
                    Id = "q-mc",
                    Type = QuestionType.MultipleChoice,
                    Prompt = "Capital of France?",
                    Options = ["Paris", "Rome"],
                    CorrectOptionIndex = 0,
                    Points = 1
                },
                new QuizQuestion
                {
                    Id = "q-sa",
                    Type = QuestionType.ShortAnswer,
                    Prompt = "Define RAG.",
                    ExpectedAnswer = "Retrieval augmented generation",
                    Points = 1
                }
            ]
        };
        return await _quizzes.AddAsync(quiz, CancellationToken.None);
    }

    #endregion
}
