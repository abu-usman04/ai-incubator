namespace AiIncubator.Server.Models.RequestModels.Quiz;

public class SubmitAnswerRequest
{
    public required string QuestionId { get; init; }

    public int? SelectedOptionIndex { get; init; }

    public string? Text { get; init; }
}
