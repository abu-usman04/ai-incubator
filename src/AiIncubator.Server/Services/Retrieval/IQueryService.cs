using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Retrieval;

public interface IQueryService
{
    Task<RagAnswer> AnswerAsync(string question, int topK, CancellationToken cancellationToken);

    /// <summary>
    /// Embeds the query and returns the grounded chunks from the vector store, without prompting the
    /// chat model. Reused by features (e.g. quiz generation) that need raw retrieval context.
    /// </summary>
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, int topK, CancellationToken cancellationToken);
}
