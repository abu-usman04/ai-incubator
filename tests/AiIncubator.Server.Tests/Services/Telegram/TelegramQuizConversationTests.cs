namespace AiIncubator.Server.Tests.Services.Telegram;

using FluentAssertions;
using Moq;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.Quiz;
using AiIncubator.Server.Services.Quizzes;
using AiIncubator.Server.Services.Telegram;

public class TelegramQuizConversationTests
{
    private readonly Mock<IQuizService> _quiz = new();
    private readonly Mock<ITelegramService> _telegram = new();
    private readonly List<string> _sent = [];
    private readonly TelegramQuizConversation _conversation;

    public TelegramQuizConversationTests()
    {
        _telegram
            .Setup(t => t.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<long, string, CancellationToken>((_, text, _) => _sent.Add(text))
            .Returns(Task.CompletedTask);
        _conversation = new TelegramQuizConversation(
            _quiz.Object, _telegram.Object, new InMemoryQuizConversationStore());
    }

    [Fact]
    public async Task StartCommand_StartsAttempt_AndSendsFirstQuestion()
    {
        StubQuizAndAttempt();

        bool handled = await _conversation.HandleAsync(100, "/quiz quiz-1", CancellationToken.None);

        handled.Should().BeTrue();
        _conversation.IsActive(100).Should().BeTrue();
        _sent.Should().ContainSingle().Which.Should().Contain("Capital of France?").And.Contain("1. Paris");
        _quiz.Verify(q => q.StartAttemptAsync("quiz-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NumberedReply_SubmitsCorrectOptionIndex_AndReportsScore()
    {
        StubQuizAndAttempt();
        await _conversation.HandleAsync(100, "/quiz quiz-1", CancellationToken.None);

        SubmitAnswerRequest? captured = null;
        _quiz.Setup(q => q.SubmitAnswerAsync("attempt-1", It.IsAny<SubmitAnswerRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, SubmitAnswerRequest, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(CompletedAttempt());

        await _conversation.HandleAsync(100, "1", CancellationToken.None);

        captured!.SelectedOptionIndex.Should().Be(0);
        _sent.Should().Contain(text => text.Contains("Score: 1/1"));
        _sent.Should().Contain(text => text.Contains("Quiz complete"));
        _conversation.IsActive(100).Should().BeFalse();
    }

    private void StubQuizAndAttempt()
    {
        var quiz = new Quiz
        {
            Id = "quiz-1",
            Title = "Geo",
            Topic = "geo",
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
                }
            ]
        };
        _quiz.Setup(q => q.GetAsync("quiz-1", It.IsAny<CancellationToken>())).ReturnsAsync(quiz);
        _quiz.Setup(q => q.StartAttemptAsync("quiz-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuizAttempt
            {
                Id = "attempt-1",
                QuizId = "quiz-1",
                Status = AttemptStatus.InProgress,
                MaxScore = 1,
                StartedAt = DateTime.UtcNow
            });
    }

    private static QuizAttempt CompletedAttempt() => new()
    {
        Id = "attempt-1",
        QuizId = "quiz-1",
        Status = AttemptStatus.Completed,
        MaxScore = 1,
        Score = 1,
        StartedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow,
        Answers =
        {
            new QuizAnswer { QuestionId = "q-mc", SelectedOptionIndex = 0, IsCorrect = true, AwardedPoints = 1, Feedback = "Correct." }
        }
    };
}
