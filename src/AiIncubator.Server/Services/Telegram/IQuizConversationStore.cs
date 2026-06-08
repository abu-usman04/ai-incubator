namespace AiIncubator.Server.Services.Telegram;

/// <summary>
/// Singleton store for in-progress Telegram quiz state, so a scoped conversation can persist
/// progress across separate incoming updates.
/// </summary>
public interface IQuizConversationStore
{
    QuizConversationState? Get(long chatId);

    void Set(long chatId, QuizConversationState state);

    void Remove(long chatId);
}
