namespace AiIncubator.Server.Services.Telegram;

/// <summary>Thin send seam over the Telegram Bot API. Mocked in tests; no-op when the bot is disabled.</summary>
public interface ITelegramService
{
    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken);
}
