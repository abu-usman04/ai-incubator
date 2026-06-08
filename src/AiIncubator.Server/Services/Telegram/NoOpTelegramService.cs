namespace AiIncubator.Server.Services.Telegram;

/// <summary>Used when the bot is disabled (no token), so the app and tests run without a live bot.</summary>
public class NoOpTelegramService : ITelegramService
{
    public Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
