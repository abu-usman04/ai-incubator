namespace AiIncubator.Server.Models.Domain;

/// <summary>Maps a Telegram chat to a Clerk user so reminders and quizzes reach the right person.</summary>
public class TelegramChatLink
{
    public required long ChatId { get; init; }

    public required string UserId { get; init; }

    public DateTime LinkedAt { get; init; }
}
