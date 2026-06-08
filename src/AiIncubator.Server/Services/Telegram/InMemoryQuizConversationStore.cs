using System.Collections.Concurrent;

namespace AiIncubator.Server.Services.Telegram;

public class InMemoryQuizConversationStore : IQuizConversationStore
{
    #region Private fields region

    private readonly ConcurrentDictionary<long, QuizConversationState> _states = new();

    #endregion

    #region Public methods region

    public QuizConversationState? Get(long chatId)
    {
        _states.TryGetValue(chatId, out QuizConversationState? state);
        return state;
    }

    public void Set(long chatId, QuizConversationState state)
    {
        _states[chatId] = state;
    }

    public void Remove(long chatId)
    {
        _states.TryRemove(chatId, out _);
    }

    #endregion
}
