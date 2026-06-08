using AiIncubator.Server.Common.Enums;

namespace AiIncubator.Server.Models.Domain;

public class Quiz
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Topic { get; init; }

    public string? ModuleId { get; init; }

    public QuizStatus Status { get; init; }

    public IReadOnlyList<QuizQuestion> Questions { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}
