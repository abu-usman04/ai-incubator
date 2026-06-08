namespace AiIncubator.Server.Services.Telegram;

/// <summary>Routes a normalized incoming Telegram message (chat id + text) to the right handler.</summary>
public interface ITelegramUpdateHandler
{
    Task HandleAsync(long chatId, string text, CancellationToken cancellationToken);
}
