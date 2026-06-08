namespace AiIncubator.Server.Services.Telegram;

/// <summary>Drives a quiz interactively over Telegram for a single chat.</summary>
public interface ITelegramQuizConversation
{
    /// <summary>True if a quiz is currently in progress for the chat.</summary>
    bool IsActive(long chatId);

    /// <summary>
    /// Handles a message for the quiz flow. Returns true when the message was consumed by the quiz
    /// (a <c>/quiz</c> command or an answer to an in-progress quiz), false otherwise.
    /// </summary>
    Task<bool> HandleAsync(long chatId, string text, CancellationToken cancellationToken);
}
