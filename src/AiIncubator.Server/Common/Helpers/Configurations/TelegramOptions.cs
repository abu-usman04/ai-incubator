namespace AiIncubator.Server.Common.Helpers.Configurations;

public class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Bot token from BotFather. Supplied via .env / user-secrets only, never committed.</summary>
    public string BotToken { get; init; } = string.Empty;

    /// <summary>When false (default) the bot client, polling and reminder worker are not started.</summary>
    public bool Enabled { get; init; }

    /// <summary>How often the reminder worker checks for due plan tasks.</summary>
    public int ReminderIntervalSeconds { get; init; } = 300;
}
