using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Helpers.Configurations;

namespace AiIncubator.Server.Services.Telegram;

/// <summary>
/// Periodically dispatches reminders for due plan tasks. Registered only when the bot is enabled.
/// The dispatch logic lives in <see cref="IReminderDispatcher"/> so it stays unit-testable.
/// </summary>
public class ReminderBackgroundService(
    IReminderDispatcher dispatcher,
    IOptions<TelegramOptions> options,
    ILogger<ReminderBackgroundService> logger) : BackgroundService
{
    #region Private fields region

    private readonly TimeSpan _interval = TimeSpan.FromSeconds(Math.Max(30, options.Value.ReminderIntervalSeconds));

    #endregion

    #region Protected methods region

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await dispatcher.DispatchDueRemindersAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reminder dispatch failed");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    #endregion
}
