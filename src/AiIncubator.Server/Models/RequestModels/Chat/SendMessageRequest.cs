namespace AiIncubator.Server.Models.RequestModels.Chat;

public class SendMessageRequest
{
    public string Message { get; init; } = string.Empty;

    public int TopK { get; init; }
}
