using AiIncubator.Server.Services.Telegram;
using Telegram.Bot;

namespace AiIncubator.Server.Common.Helpers.Configurations;

public static class TelegramServicesRegistration
{
    public static IServiceCollection AddTelegramServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));
        TelegramOptions options =
            configuration.GetSection(TelegramOptions.SectionName).Get<TelegramOptions>() ?? new TelegramOptions();

        services.AddSingleton<ITelegramChatLinkRepository, InMemoryTelegramChatLinkRepository>();
        services.AddSingleton<IQuizConversationStore, InMemoryQuizConversationStore>();
        services.AddSingleton<IReminderDispatcher, ReminderDispatcher>();
        services.AddScoped<ITelegramQuizConversation, TelegramQuizConversation>();
        services.AddScoped<ITelegramUpdateHandler, TelegramUpdateHandler>();

        bool enabled = options.Enabled && !string.IsNullOrWhiteSpace(options.BotToken);
        if (enabled)
        {
            services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(options.BotToken));
            services.AddSingleton<ITelegramService, TelegramService>();
            services.AddHostedService<ReminderBackgroundService>();
            services.AddHostedService<TelegramReceiverService>();
        }
        else
        {
            services.AddSingleton<ITelegramService, NoOpTelegramService>();
        }

        return services;
    }
}
