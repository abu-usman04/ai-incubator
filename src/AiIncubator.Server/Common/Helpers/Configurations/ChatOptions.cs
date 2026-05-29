namespace AiIncubator.Server.Common.Helpers.Configurations;

public class ChatOptions
{
    public const string SectionName = "Chat";
    public string BaseUrl { get; init; } = "https://api.z.ai/api/paas/v4/";
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "glm-5.1";
    public int TimeoutSeconds { get; init; } = 60;
}
