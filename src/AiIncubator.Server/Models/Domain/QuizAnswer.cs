namespace AiIncubator.Server.Models.Domain;

public class QuizAnswer
{
    public required string QuestionId { get; init; }

    public int? SelectedOptionIndex { get; init; }

    public string? Text { get; init; }

    public bool IsCorrect { get; init; }

    public int AwardedPoints { get; init; }

    public string? Feedback { get; init; }
}
