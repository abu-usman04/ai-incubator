using Telegram.Bot;
using Telegram.Bot.Types;

namespace AiIncubator.Server.Services.Telegram;

/// <summary>
/// Long-polls the Telegram Bot API for updates and forwards each text message to the update handler.
/// Registered only when the bot is enabled, so tests and CI never open a connection. The handler is
/// resolved per update from a fresh scope so it can use scoped services (quiz/plan).
/// </summary>
public class TelegramReceiverService(
    ITelegramBotClient bot,
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramReceiverService> logger) : BackgroundService
{
    #region Private fields region

    private const int PollTimeoutSeconds = 30;
    private int _offset;

    #endregion

    #region Protected methods region

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Update[] updates = await bot.GetUpdates(
                    offset: _offset, timeout: PollTimeoutSeconds, cancellationToken: stoppingToken);

                foreach (Update update in updates)
                {
                    _offset = update.Id + 1;
                    await DispatchAsync(update, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Telegram receive failed");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    #endregion

    #region Private methods region

    private async Task DispatchAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message?.Text is not { } text)
        {
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ITelegramUpdateHandler>();
        await handler.HandleAsync(update.Message.Chat.Id, text, cancellationToken);
    }

    #endregion
}
