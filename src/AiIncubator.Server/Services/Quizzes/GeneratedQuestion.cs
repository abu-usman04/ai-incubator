namespace AiIncubator.Server.Services.Quizzes;

/// <summary>Shape of a single question returned by the GLM <c>submit_quiz</c> tool call.</summary>
internal sealed class GeneratedQuestion
{
    public string Type { get; init; } = "multiple_choice";

    public string Prompt { get; init; } = string.Empty;

    public List<string> Options { get; init; } = [];

    public int? CorrectOptionIndex { get; init; }

    public string? ExpectedAnswer { get; init; }
}
