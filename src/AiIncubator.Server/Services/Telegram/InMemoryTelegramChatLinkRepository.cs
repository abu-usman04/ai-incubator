using System.Collections.Concurrent;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Telegram;

public class InMemoryTelegramChatLinkRepository : ITelegramChatLinkRepository
{
    #region Private fields region

    private readonly ConcurrentDictionary<long, TelegramChatLink> _links = new();

    #endregion

    #region Public methods region

    public Task LinkAsync(long chatId, string userId, CancellationToken cancellationToken)
    {
        _links[chatId] = new TelegramChatLink
        {
            ChatId = chatId,
            UserId = userId,
            LinkedAt = DateTime.UtcNow
        };
        return Task.CompletedTask;
    }

    public Task<string?> GetUserIdAsync(long chatId, CancellationToken cancellationToken)
    {
        _links.TryGetValue(chatId, out TelegramChatLink? link);
        return Task.FromResult(link?.UserId);
    }

    public Task<IReadOnlyList<long>> GetChatIdsForUserAsync(string userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<long> chatIds = _links.Values
            .Where(link => link.UserId == userId)
            .Select(link => link.ChatId)
            .ToList();

        return Task.FromResult(chatIds);
    }

    public Task<IReadOnlyList<TelegramChatLink>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TelegramChatLink> items = _links.Values
            .OrderByDescending(link => link.LinkedAt)
            .ToList();

        return Task.FromResult(items);
    }

    #endregion
}
