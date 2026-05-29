namespace AiIncubator.Server.Models.Domain;

public class ChatSession
{
    public required string Id { get; init; }

    public required string Title { get; set; }

    public List<ChatMessage> Messages { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}
