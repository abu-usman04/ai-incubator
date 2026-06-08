using System.Collections.Concurrent;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.LearningPlans;

namespace AiIncubator.Server.Services.Telegram;

/// <summary>
/// Finds due plan tasks and sends one Telegram reminder per linked chat. Tracks which tasks have
/// already been reminded in-memory so a task is not reminded twice across interval ticks.
/// </summary>
public class ReminderDispatcher(
    ILearningPlanRepository plans,
    ITelegramChatLinkRepository links,
    ITelegramService telegram) : IReminderDispatcher
{
    #region Private fields region

    private readonly ConcurrentDictionary<string, byte> _reminded = new();

    #endregion

    #region Public methods region

    public async Task<int> DispatchDueRemindersAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        IReadOnlyList<DuePlanTask> due = await plans.ListDueTasksAsync(nowUtc, cancellationToken);
        int sent = 0;

        foreach (DuePlanTask item in due)
        {
            if (string.IsNullOrWhiteSpace(item.UserId) || !_reminded.TryAdd(item.Task.Id, 0))
            {
                continue;
            }

            IReadOnlyList<long> chatIds = await links.GetChatIdsForUserAsync(item.UserId, cancellationToken);
            foreach (long chatId in chatIds)
            {
                await telegram.SendMessageAsync(chatId, BuildReminder(item.Task), cancellationToken);
                sent++;
            }
        }

        return sent;
    }

    #endregion

    #region Private methods region

    private static string BuildReminder(PlanTask task)
    {
        return $"Reminder: \"{task.Title}\" is due. {task.Description}".Trim();
    }

    #endregion
}
