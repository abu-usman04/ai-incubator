using System.Net;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Chat;
using AiIncubator.Server.Services.Embeddings;
using AiIncubator.Server.Services.VectorStore;

namespace AiIncubator.Server.Services.Retrieval;

public class QueryService(
    IEmbeddingClient embeddings,
    IVectorStore vectorStore,
    IChatCompletionClient chat,
    RagPromptBuilder promptBuilder,
    IOptions<RagOptions> ragOptions) : IQueryService
{
    #region Fields

    private const int MinTopK = 1;
    private const int MaxTopK = 20;
    private const string NoAnswer = "I don't know.";
    private readonly int _defaultTopK = ragOptions.Value.DefaultTopK;

    #endregion

    #region Public methods

    public async Task<RagAnswer> AnswerAsync(string question, int topK, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new AppException(HttpStatusCode.BadRequest, "question is required.", "BAD_REQUEST");
        }

        int effectiveTopK = ResolveTopK(topK);
        float[] queryVector = await embeddings.EmbedAsync(question, cancellationToken);
        IReadOnlyList<RetrievedChunk> sources = await vectorStore.SearchAsync(
            queryVector, effectiveTopK, cancellationToken);

        if (sources.Count == 0)
        {
            return new RagAnswer(NoAnswer, sources);
        }

        RagPrompt prompt = promptBuilder.Build(question, sources);
        string answer = await chat.CompleteAsync(prompt.System, prompt.User, cancellationToken);
        return new RagAnswer(answer, sources);
    }

    #endregion

    #region Private methods

    private int ResolveTopK(int requested)
    {
        int value = requested <= 0 ? _defaultTopK : requested;
        return Math.Clamp(value, MinTopK, MaxTopK);
    }

    #endregion
}
