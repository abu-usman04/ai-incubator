using AiIncubator.Server.Models.Glm;

namespace AiIncubator.Server.Services.Chat;

public interface IChatCompletionClient
{
    /// <summary>
    /// High-level facade used by the RAG layer: builds a 2-message (system + user) request and
    /// returns the assistant content from the first choice.
    /// </summary>
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);

    /// <summary>
    /// Full-fidelity call exposing the entire GLM chat surface (tools, thinking, multi-turn).
    /// Use this when the simple facade is not enough.
    /// </summary>
    Task<GlmChatResponse> ChatAsync(GlmChatRequest request, CancellationToken cancellationToken);
}
