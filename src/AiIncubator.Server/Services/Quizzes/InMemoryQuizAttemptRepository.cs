using System.Collections.Concurrent;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Quizzes;

public class InMemoryQuizAttemptRepository : IQuizAttemptRepository
{
    #region Private fields region

    private readonly ConcurrentDictionary<string, QuizAttempt> _attempts = new();

    #endregion

    #region Public methods region

    public Task<QuizAttempt> AddAsync(QuizAttempt attempt, CancellationToken cancellationToken)
    {
        _attempts[attempt.Id] = attempt;
        return Task.FromResult(attempt);
    }

    public Task<QuizAttempt?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _attempts.TryGetValue(id, out QuizAttempt? attempt);
        return Task.FromResult(attempt);
    }

    public Task UpdateAsync(QuizAttempt attempt, CancellationToken cancellationToken)
    {
        _attempts[attempt.Id] = attempt;
        return Task.CompletedTask;
    }

    #endregion
}
