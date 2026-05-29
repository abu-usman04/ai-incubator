using System.Net;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Retrieval;

namespace AiIncubator.Server.Services.Chat.Sessions;

public class ChatService(IChatSessionStore store, IQueryService queryService) : IChatService
{
    #region Private fields region

    private const string DefaultTitle = "New conversation";

    #endregion

    #region Public methods region

    public async Task<ChatSession> CreateSessionAsync(string? title, CancellationToken cancellationToken)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = string.IsNullOrWhiteSpace(title) ? DefaultTitle : title.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return await store.CreateAsync(session, cancellationToken);
    }

    public Task<IReadOnlyList<ChatSession>> ListSessionsAsync(CancellationToken cancellationToken)
    {
        return store.ListAsync(cancellationToken);
    }

    public async Task<ChatSession> GetSessionAsync(string id, CancellationToken cancellationToken)
    {
        ChatSession? session = await store.GetByIdAsync(id, cancellationToken);
        if (session is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Chat session '{id}' not found.", "NOT_FOUND");
        }

        return session;
    }

    public async Task<ChatMessage> SendMessageAsync(
        string sessionId,
        string message,
        int topK,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Message is required.", "BAD_REQUEST");
        }

        await GetSessionAsync(sessionId, cancellationToken);

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            Role = ChatRole.User,
            Content = message,
            CreatedAt = DateTime.UtcNow
        };
        await store.AppendMessageAsync(sessionId, userMessage, cancellationToken);

        RagAnswer answer = await queryService.AnswerAsync(message, topK, cancellationToken);

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            Role = ChatRole.Assistant,
            Content = answer.Answer,
            Sources = answer.Sources,
            CreatedAt = DateTime.UtcNow
        };
        await store.AppendMessageAsync(sessionId, assistantMessage, cancellationToken);

        return assistantMessage;
    }

    #endregion
}
