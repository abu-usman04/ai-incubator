using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Chat.Sessions;

public interface IChatSessionStore
{
    Task<ChatSession> CreateAsync(ChatSession session, CancellationToken cancellationToken);

    Task<ChatSession?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatSession>> ListAsync(CancellationToken cancellationToken);

    Task AppendMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken);
}
