using AiIncubator.Server.Common.Enums;

namespace AiIncubator.Server.Models.Domain;

public class ChatMessage
{
    public required string Id { get; init; }

    public required ChatRole Role { get; init; }

    public required string Content { get; init; }

    public IReadOnlyList<RetrievedChunk> Sources { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}
