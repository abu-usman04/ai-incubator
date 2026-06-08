using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Quizzes;

public interface IQuizRepository
{
    Task<Quiz> AddAsync(Quiz quiz, CancellationToken cancellationToken);

    Task<Quiz?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Quiz>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);
}
