namespace AiIncubator.Server.Services.Telegram;

public class TelegramUpdateHandler(
    ITelegramQuizConversation quizConversation,
    ITelegramChatLinkRepository links,
    ITelegramService telegram) : ITelegramUpdateHandler
{
    #region Private fields region

    private const string LinkCommand = "/link";
    private const string HelpText =
        "Commands:\n/link <userId> — link this chat to your account\n/quiz <quizId> — take a quiz";

    #endregion

    #region Public methods region

    public async Task HandleAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        string trimmed = (text ?? string.Empty).Trim();

        if (quizConversation.IsActive(chatId) || trimmed.StartsWith("/quiz", StringComparison.OrdinalIgnoreCase))
        {
            bool handled = await quizConversation.HandleAsync(chatId, trimmed, cancellationToken);
            if (handled)
            {
                return;
            }
        }

        if (trimmed.StartsWith(LinkCommand, StringComparison.OrdinalIgnoreCase))
        {
            await LinkAsync(chatId, trimmed, cancellationToken);
            return;
        }

        await telegram.SendMessageAsync(chatId, HelpText, cancellationToken);
    }

    #endregion

    #region Private methods region

    private async Task LinkAsync(long chatId, string command, CancellationToken cancellationToken)
    {
        string userId = command[LinkCommand.Length..].Trim();
        if (string.IsNullOrWhiteSpace(userId))
        {
            await telegram.SendMessageAsync(chatId, "Usage: /link <userId>", cancellationToken);
            return;
        }

        await links.LinkAsync(chatId, userId, cancellationToken);
        await telegram.SendMessageAsync(chatId, "This chat is now linked. You will receive reminders here.", cancellationToken);
    }

    #endregion
}
