using AiIncubator.Server.Common.Enums;

namespace AiIncubator.Server.Models.Domain;

public class QuizQuestion
{
    public required string Id { get; init; }

    public required QuestionType Type { get; init; }

    public required string Prompt { get; init; }

    public IReadOnlyList<string> Options { get; init; } = [];

    public int? CorrectOptionIndex { get; init; }

    public string? ExpectedAnswer { get; init; }

    public int Points { get; init; } = 1;
}
