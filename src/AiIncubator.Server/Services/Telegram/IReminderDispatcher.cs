namespace AiIncubator.Server.Services.Telegram;

public interface IReminderDispatcher
{
    /// <summary>Sends a reminder for each due, not-yet-reminded plan task. Returns the number sent.</summary>
    Task<int> DispatchDueRemindersAsync(DateTime nowUtc, CancellationToken cancellationToken);
}
