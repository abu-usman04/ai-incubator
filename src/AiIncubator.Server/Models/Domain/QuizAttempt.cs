using AiIncubator.Server.Common.Enums;

namespace AiIncubator.Server.Models.Domain;

public class QuizAttempt
{
    public required string Id { get; init; }

    public required string QuizId { get; init; }

    public AttemptStatus Status { get; set; }

    public IList<QuizAnswer> Answers { get; init; } = [];

    public int Score { get; set; }

    public int MaxScore { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime? CompletedAt { get; set; }
}
