using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Telegram;

/// <summary>In-memory progress of a Telegram quiz: which quiz, which attempt, and the current question.</summary>
public class QuizConversationState
{
    public required Quiz Quiz { get; init; }

    public required string AttemptId { get; init; }

    public int Index { get; set; }
}
