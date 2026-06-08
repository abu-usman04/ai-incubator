namespace AiIncubator.Server.Services.Quizzes;

/// <summary>Shape of the arguments returned by the GLM <c>grade_answer</c> tool call.</summary>
internal sealed class GradedAnswer
{
    public bool IsCorrect { get; init; }

    public int Points { get; init; }

    public string? Feedback { get; init; }
}
