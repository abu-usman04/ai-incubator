namespace AiIncubator.Server.Models.RequestModels.Quiz;

public class GenerateQuizRequest
{
    public string? Topic { get; init; }

    public string? ModuleId { get; init; }

    public int QuestionCount { get; init; } = 5;

    public int TopK { get; init; }

    public bool IncludeShortAnswer { get; init; } = true;
}
