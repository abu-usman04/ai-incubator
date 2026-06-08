using System.Net;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.Glm;
using AiIncubator.Server.Models.RequestModels.Quiz;
using AiIncubator.Server.Services.Chat;
using AiIncubator.Server.Services.Modules;
using AiIncubator.Server.Services.Retrieval;

namespace AiIncubator.Server.Services.Quizzes;

public class QuizService(
    IQueryService queryService,
    IChatCompletionClient chat,
    IQuizRepository quizzes,
    IQuizAttemptRepository attempts,
    IModuleService modules,
    QuizPromptBuilder promptBuilder,
    IOptions<RagOptions> ragOptions) : IQuizService
{
    #region Private fields region

    private const int MinQuestions = 1;
    private const int MaxQuestions = 20;
    private readonly int _defaultTopK = ragOptions.Value.DefaultTopK;

    #endregion

    #region Public methods region

    public async Task<Quiz> GenerateAsync(GenerateQuizRequest request, CancellationToken cancellationToken)
    {
        string topic = await ResolveTopicAsync(request, cancellationToken);
        int questionCount = Math.Clamp(request.QuestionCount, MinQuestions, MaxQuestions);
        int topK = request.TopK <= 0 ? _defaultTopK : request.TopK;

        IReadOnlyList<RetrievedChunk> chunks = await queryService.RetrieveAsync(topic, topK, cancellationToken);
        if (chunks.Count == 0)
        {
            throw new AppException(
                HttpStatusCode.BadRequest, "No indexed content to build a quiz from.", "BAD_REQUEST");
        }

        GlmChatRequest generation = promptBuilder.BuildGenerationRequest(
            topic, chunks, questionCount, request.IncludeShortAnswer);
        GlmChatResponse response = await chat.ChatAsync(generation, cancellationToken);

        GeneratedQuiz? generated = GlmToolCallParser.Parse<GeneratedQuiz>(response, "submit_quiz");
        if (generated is null || generated.Questions.Count == 0)
        {
            throw new AppException(
                HttpStatusCode.InternalServerError, "Quiz generation returned no questions.", "INTERNAL_ERROR");
        }

        var quiz = new Quiz
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = topic,
            Topic = topic,
            ModuleId = request.ModuleId,
            Status = QuizStatus.Ready,
            CreatedAt = DateTime.UtcNow,
            Questions = generated.Questions.Select(MapQuestion).ToList()
        };

        return await quizzes.AddAsync(quiz, cancellationToken);
    }

    public async Task<Quiz> GetAsync(string id, CancellationToken cancellationToken)
    {
        Quiz? quiz = await quizzes.GetByIdAsync(id, cancellationToken);
        return quiz ?? throw new AppException(HttpStatusCode.NotFound, $"Quiz '{id}' not found.", "NOT_FOUND");
    }

    public async Task<PagedResult<Quiz>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        int safePage = page < 1 ? 1 : page;
        int safePageSize = Math.Clamp(pageSize, 1, 100);

        IReadOnlyList<Quiz> items = await quizzes.ListAsync(safePage, safePageSize, cancellationToken);
        int total = await quizzes.CountAsync(cancellationToken);

        return new PagedResult<Quiz>
        {
            Items = items,
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = total
        };
    }

    public async Task<QuizAttempt> StartAttemptAsync(string quizId, CancellationToken cancellationToken)
    {
        Quiz quiz = await GetAsync(quizId, cancellationToken);

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid().ToString("N"),
            QuizId = quiz.Id,
            Status = AttemptStatus.InProgress,
            MaxScore = quiz.Questions.Sum(question => question.Points),
            StartedAt = DateTime.UtcNow
        };

        return await attempts.AddAsync(attempt, cancellationToken);
    }

    public async Task<QuizAttempt> SubmitAnswerAsync(
        string attemptId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        QuizAttempt attempt = await GetAttemptAsync(attemptId, cancellationToken);
        if (attempt.Status == AttemptStatus.Completed)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Attempt is already completed.", "BAD_REQUEST");
        }

        Quiz quiz = await GetAsync(attempt.QuizId, cancellationToken);
        QuizQuestion question = FindQuestion(quiz, request.QuestionId, attempt);

        QuizAnswer answer = await GradeAsync(question, request, cancellationToken);
        attempt.Answers.Add(answer);
        attempt.Score += answer.AwardedPoints;

        if (attempt.Answers.Count >= quiz.Questions.Count)
        {
            attempt.Status = AttemptStatus.Completed;
            attempt.CompletedAt = DateTime.UtcNow;
        }

        await attempts.UpdateAsync(attempt, cancellationToken);
        return attempt;
    }

    public async Task<QuizAttempt> GetAttemptAsync(string attemptId, CancellationToken cancellationToken)
    {
        QuizAttempt? attempt = await attempts.GetByIdAsync(attemptId, cancellationToken);
        return attempt
               ?? throw new AppException(HttpStatusCode.NotFound, $"Attempt '{attemptId}' not found.", "NOT_FOUND");
    }

    #endregion

    #region Private methods region

    private async Task<string> ResolveTopicAsync(GenerateQuizRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Topic))
        {
            return request.Topic.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ModuleId))
        {
            KnowledgeModule module = await modules.GetAsync(request.ModuleId, cancellationToken);
            return module.Name;
        }

        throw new AppException(HttpStatusCode.BadRequest, "topic or moduleId is required.", "BAD_REQUEST");
    }

    private async Task<QuizAnswer> GradeAsync(
        QuizQuestion question,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        if (question.Type == QuestionType.MultipleChoice)
        {
            return GradeMultipleChoice(question, request);
        }

        return await GradeShortAnswerAsync(question, request.Text ?? string.Empty, cancellationToken);
    }

    private static QuizAnswer GradeMultipleChoice(QuizQuestion question, SubmitAnswerRequest request)
    {
        bool correct = request.SelectedOptionIndex is not null
                       && request.SelectedOptionIndex == question.CorrectOptionIndex;

        return new QuizAnswer
        {
            QuestionId = question.Id,
            SelectedOptionIndex = request.SelectedOptionIndex,
            IsCorrect = correct,
            AwardedPoints = correct ? question.Points : 0,
            Feedback = correct ? "Correct." : "Incorrect."
        };
    }

    private async Task<QuizAnswer> GradeShortAnswerAsync(
        QuizQuestion question,
        string answerText,
        CancellationToken cancellationToken)
    {
        GlmChatRequest grading = promptBuilder.BuildGradingRequest(question, answerText);
        GlmChatResponse response = await chat.ChatAsync(grading, cancellationToken);
        GradedAnswer? graded = GlmToolCallParser.Parse<GradedAnswer>(response, "grade_answer");

        int points = graded is null ? 0 : Math.Clamp(graded.Points, 0, question.Points);
        return new QuizAnswer
        {
            QuestionId = question.Id,
            Text = answerText,
            IsCorrect = graded?.IsCorrect ?? false,
            AwardedPoints = points,
            Feedback = graded?.Feedback ?? "Could not grade this answer automatically."
        };
    }

    private static QuizQuestion FindQuestion(Quiz quiz, string questionId, QuizAttempt attempt)
    {
        if (attempt.Answers.Any(answer => answer.QuestionId == questionId))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Question already answered.", "BAD_REQUEST");
        }

        QuizQuestion? question = quiz.Questions.FirstOrDefault(item => item.Id == questionId);
        return question
               ?? throw new AppException(HttpStatusCode.BadRequest, $"Unknown question '{questionId}'.", "BAD_REQUEST");
    }

    private static QuizQuestion MapQuestion(GeneratedQuestion generated) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = generated.Type.Contains("short", StringComparison.OrdinalIgnoreCase)
            ? QuestionType.ShortAnswer
            : QuestionType.MultipleChoice,
        Prompt = generated.Prompt,
        Options = generated.Options,
        CorrectOptionIndex = generated.CorrectOptionIndex,
        ExpectedAnswer = generated.ExpectedAnswer,
        Points = 1
    };

    #endregion
}
