using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Chat.Sessions;

public interface IChatService
{
    Task<ChatSession> CreateSessionAsync(string? title, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatSession>> ListSessionsAsync(CancellationToken cancellationToken);

    Task<ChatSession> GetSessionAsync(string id, CancellationToken cancellationToken);

    Task<ChatMessage> SendMessageAsync(string sessionId, string message, int topK, CancellationToken cancellationToken);
}
