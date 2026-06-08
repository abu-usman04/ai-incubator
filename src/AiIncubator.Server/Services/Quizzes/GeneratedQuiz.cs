namespace AiIncubator.Server.Services.Quizzes;

/// <summary>Shape of the arguments returned by the GLM <c>submit_quiz</c> tool call.</summary>
internal sealed class GeneratedQuiz
{
    public List<GeneratedQuestion> Questions { get; init; } = [];
}
