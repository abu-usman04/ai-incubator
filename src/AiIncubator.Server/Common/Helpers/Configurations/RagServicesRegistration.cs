using Microsoft.Extensions.Options;
using AiIncubator.Server.Services.Chat;
using AiIncubator.Server.Services.Chat.Sessions;
using AiIncubator.Server.Services.Documents;
using AiIncubator.Server.Services.Embeddings;
using AiIncubator.Server.Services.Ingestion;
using AiIncubator.Server.Services.LearningPlans;
using AiIncubator.Server.Services.Modules;
using AiIncubator.Server.Services.Quizzes;
using AiIncubator.Server.Services.Retrieval;
using AiIncubator.Server.Services.VectorStore;
using Qdrant.Client;

namespace AiIncubator.Server.Common.Helpers.Configurations;

public static class RagServicesRegistration
{
    public static IServiceCollection AddRagServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName))
            .Configure<ChatOptions>(configuration.GetSection(ChatOptions.SectionName))
            .Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName))
            .Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName))
            .Configure<DocumentsOptions>(configuration.GetSection(DocumentsOptions.SectionName));

        services.AddHttpClient<IEmbeddingClient, BgeLargeEmbeddingClient>((sp, client) =>
        {
            EmbeddingOptions opts = sp.GetRequiredService<IOptions<EmbeddingOptions>>().Value;
            client.BaseAddress = new Uri(EnsureTrailingSlash(opts.ServerUrl));
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        services.AddHttpClient<IChatCompletionClient, GlmChatClient>((sp, client) =>
        {
            ChatOptions opts = sp.GetRequiredService<IOptions<ChatOptions>>().Value;
            client.BaseAddress = new Uri(EnsureTrailingSlash(opts.BaseUrl));
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        services.AddSingleton<QdrantClient>(sp =>
        {
            QdrantOptions opts = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
            return new QdrantClient(opts.Host, opts.Port, opts.Https, opts.ApiKey ?? string.Empty);
        });

        services.AddSingleton<ITextSplitter, CharacterTextSplitter>();
        services.AddSingleton<RagPromptBuilder>();
        services.AddSingleton<IVectorStore, QdrantVectorStore>();
        services.AddScoped<IIngestionService, IngestionService>();
        services.AddScoped<IQueryService, QueryService>();

        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.AddScoped<IDocumentService, DocumentService>();

        services.AddSingleton<IModuleRepository, InMemoryModuleRepository>();
        services.AddScoped<IModuleService, ModuleService>();

        services.AddSingleton<IChatSessionStore, InMemoryChatSessionStore>();
        services.AddScoped<IChatService, ChatService>();

        services.AddSingleton<QuizPromptBuilder>();
        services.AddSingleton<IQuizRepository, InMemoryQuizRepository>();
        services.AddSingleton<IQuizAttemptRepository, InMemoryQuizAttemptRepository>();
        services.AddScoped<IQuizService, QuizService>();

        services.AddSingleton<LearningPlanPromptBuilder>();
        services.AddSingleton<ILearningPlanRepository, InMemoryLearningPlanRepository>();
        services.AddScoped<ILearningPlanService, LearningPlanService>();

        return services;
    }

    private static string EnsureTrailingSlash(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return "/";
        }

        return url.EndsWith('/') ? url : url + "/";
    }
}
