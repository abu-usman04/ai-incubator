using System.Text;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.Quiz;
using AiIncubator.Server.Services.Quizzes;

namespace AiIncubator.Server.Services.Telegram;

public class TelegramQuizConversation(
    IQuizService quizService,
    ITelegramService telegram,
    IQuizConversationStore store) : ITelegramQuizConversation
{
    #region Private fields region

    private const string QuizCommand = "/quiz";

    #endregion

    #region Public methods region

    public bool IsActive(long chatId) => store.Get(chatId) is not null;

    public async Task<bool> HandleAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        string trimmed = text.Trim();

        if (trimmed.StartsWith(QuizCommand, StringComparison.OrdinalIgnoreCase))
        {
            await StartAsync(chatId, trimmed, cancellationToken);
            return true;
        }

        QuizConversationState? state = store.Get(chatId);
        if (state is not null)
        {
            await AnswerAsync(chatId, state, trimmed, cancellationToken);
            return true;
        }

        return false;
    }

    #endregion

    #region Private methods region

    private async Task StartAsync(long chatId, string command, CancellationToken cancellationToken)
    {
        string quizId = command[QuizCommand.Length..].Trim();
        if (string.IsNullOrWhiteSpace(quizId))
        {
            await telegram.SendMessageAsync(chatId, "Usage: /quiz <quizId>", cancellationToken);
            return;
        }

        Quiz quiz = await quizService.GetAsync(quizId, cancellationToken);
        if (quiz.Questions.Count == 0)
        {
            await telegram.SendMessageAsync(chatId, "That quiz has no questions.", cancellationToken);
            return;
        }

        QuizAttempt attempt = await quizService.StartAttemptAsync(quizId, cancellationToken);
        store.Set(chatId, new QuizConversationState { Quiz = quiz, AttemptId = attempt.Id, Index = 0 });

        await telegram.SendMessageAsync(chatId, FormatQuestion(quiz.Questions[0], 1, quiz.Questions.Count), cancellationToken);
    }

    private async Task AnswerAsync(
        long chatId,
        QuizConversationState state,
        string text,
        CancellationToken cancellationToken)
    {
        QuizQuestion question = state.Quiz.Questions[state.Index];
        SubmitAnswerRequest request = BuildAnswer(question, text);

        if (question.Type == QuestionType.MultipleChoice && request.SelectedOptionIndex is null)
        {
            await telegram.SendMessageAsync(chatId, "Please reply with the option number.", cancellationToken);
            return;
        }

        QuizAttempt attempt = await quizService.SubmitAnswerAsync(state.AttemptId, request, cancellationToken);
        QuizAnswer graded = attempt.Answers[^1];
        await telegram.SendMessageAsync(
            chatId, $"{graded.Feedback} Score: {attempt.Score}/{attempt.MaxScore}", cancellationToken);

        state.Index++;
        store.Set(chatId, state);
        if (attempt.Status == AttemptStatus.Completed || state.Index >= state.Quiz.Questions.Count)
        {
            store.Remove(chatId);
            await telegram.SendMessageAsync(
                chatId, $"Quiz complete. Final score: {attempt.Score}/{attempt.MaxScore}.", cancellationToken);
            return;
        }

        await telegram.SendMessageAsync(
            chatId,
            FormatQuestion(state.Quiz.Questions[state.Index], state.Index + 1, state.Quiz.Questions.Count),
            cancellationToken);
    }

    private static SubmitAnswerRequest BuildAnswer(QuizQuestion question, string text)
    {
        if (question.Type == QuestionType.MultipleChoice && int.TryParse(text.Trim(), out int option))
        {
            return new SubmitAnswerRequest { QuestionId = question.Id, SelectedOptionIndex = option - 1 };
        }

        return new SubmitAnswerRequest { QuestionId = question.Id, Text = text };
    }

    private static string FormatQuestion(QuizQuestion question, int number, int total)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Question {number}/{total}: {question.Prompt}");

        if (question.Type == QuestionType.MultipleChoice)
        {
            for (int i = 0; i < question.Options.Count; i++)
            {
                builder.AppendLine($"{i + 1}. {question.Options[i]}");
            }

            builder.Append("Reply with the option number.");
        }
        else
        {
            builder.Append("Reply with your answer.");
        }

        return builder.ToString();
    }

    #endregion
}
