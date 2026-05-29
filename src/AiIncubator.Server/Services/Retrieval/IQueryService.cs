using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Retrieval;

public interface IQueryService
{
    Task<RagAnswer> AnswerAsync(string question, int topK, CancellationToken cancellationToken);
}
