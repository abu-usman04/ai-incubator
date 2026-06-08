using AiIncubator.Server.Common;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.Quiz;

namespace AiIncubator.Server.Services.Quizzes;

public interface IQuizService
{
    Task<Quiz> GenerateAsync(GenerateQuizRequest request, CancellationToken cancellationToken);

    Task<Quiz> GetAsync(string id, CancellationToken cancellationToken);

    Task<PagedResult<Quiz>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<QuizAttempt> StartAttemptAsync(string quizId, CancellationToken cancellationToken);

    Task<QuizAttempt> SubmitAnswerAsync(string attemptId, SubmitAnswerRequest request, CancellationToken cancellationToken);

    Task<QuizAttempt> GetAttemptAsync(string attemptId, CancellationToken cancellationToken);
}
