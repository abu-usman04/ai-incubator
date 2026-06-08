using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Quizzes;

public interface IQuizAttemptRepository
{
    Task<QuizAttempt> AddAsync(QuizAttempt attempt, CancellationToken cancellationToken);

    Task<QuizAttempt?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task UpdateAsync(QuizAttempt attempt, CancellationToken cancellationToken);
}
