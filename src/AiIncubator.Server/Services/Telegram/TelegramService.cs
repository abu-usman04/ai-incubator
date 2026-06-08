using Telegram.Bot;

namespace AiIncubator.Server.Services.Telegram;

/// <summary>Sends messages through the Telegram Bot API. Active only when the bot is enabled.</summary>
public class TelegramService(ITelegramBotClient bot) : ITelegramService
{
    public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        await bot.SendMessage(chatId, text, cancellationToken: cancellationToken);
    }
}
