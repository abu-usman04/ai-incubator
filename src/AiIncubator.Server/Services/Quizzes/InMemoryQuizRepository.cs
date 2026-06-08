using System.Collections.Concurrent;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Quizzes;

public class InMemoryQuizRepository : IQuizRepository
{
    #region Private fields region

    private readonly ConcurrentDictionary<string, Quiz> _quizzes = new();

    #endregion

    #region Public methods region

    public Task<Quiz> AddAsync(Quiz quiz, CancellationToken cancellationToken)
    {
        _quizzes[quiz.Id] = quiz;
        return Task.FromResult(quiz);
    }

    public Task<Quiz?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _quizzes.TryGetValue(id, out Quiz? quiz);
        return Task.FromResult(quiz);
    }

    public Task<IReadOnlyList<Quiz>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        IReadOnlyList<Quiz> items = _quizzes.Values
            .OrderByDescending(quiz => quiz.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_quizzes.Count);
    }

    #endregion
}
