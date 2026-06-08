using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Telegram;

public interface ITelegramChatLinkRepository
{
    Task LinkAsync(long chatId, string userId, CancellationToken cancellationToken);

    Task<string?> GetUserIdAsync(long chatId, CancellationToken cancellationToken);

    Task<IReadOnlyList<long>> GetChatIdsForUserAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TelegramChatLink>> ListAsync(CancellationToken cancellationToken);
}
