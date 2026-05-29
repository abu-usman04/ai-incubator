using System.Collections.Concurrent;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Chat.Sessions;

public class InMemoryChatSessionStore : IChatSessionStore
{
    #region Private fields region

    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();

    #endregion

    #region Public methods region

    public Task<ChatSession> CreateAsync(ChatSession session, CancellationToken cancellationToken)
    {
        _sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task<ChatSession?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _sessions.TryGetValue(id, out ChatSession? session);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<ChatSession>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatSession> items = _sessions.Values
            .OrderByDescending(session => session.CreatedAt)
            .ToList();

        return Task.FromResult(items);
    }

    public Task AppendMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken)
    {
        if (_sessions.TryGetValue(sessionId, out ChatSession? session))
        {
            lock (session.Messages)
            {
                session.Messages.Add(message);
            }
        }

        return Task.CompletedTask;
    }

    #endregion
}
