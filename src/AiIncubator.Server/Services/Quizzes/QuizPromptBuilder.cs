using System.Text;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.Glm;

namespace AiIncubator.Server.Services.Quizzes;

/// <summary>
/// Builds the GLM tool-calling requests used to generate a grounded quiz and to grade a
/// short-answer response. Keeps the prompt and JSON-schema wiring out of <see cref="QuizService"/>.
/// </summary>
public class QuizPromptBuilder(IOptions<ChatOptions> chatOptions)
{
    #region Private fields region

    private const int MaxContextChars = 8000;
    private readonly string _model = chatOptions.Value.Model;

    #endregion

    #region Public methods region

    public GlmChatRequest BuildGenerationRequest(
        string topic,
        IReadOnlyList<RetrievedChunk> chunks,
        int questionCount,
        bool includeShortAnswer)
    {
        string context = BuildContext(chunks);
        string types = includeShortAnswer
            ? "a mix of multiple_choice and short_answer questions"
            : "only multiple_choice questions";

        string system =
            "You write quiz questions to test understanding of the provided CONTEXT. " +
            "Use ONLY facts present in the context; never invent details. " +
            "For multiple_choice include 3-4 plausible options and set correctOptionIndex. " +
            "For short_answer set expectedAnswer to the ideal concise answer. " +
            "Call the submit_quiz function exactly once with the questions.";

        string user =
            $"Topic: {topic}\nCreate {questionCount} {types}.\n\nCONTEXT:\n{context}";

        return new GlmChatRequest
        {
            Model = _model,
            Temperature = 0.4,
            Messages =
            [
                new GlmMessage { Role = "system", Content = system },
                new GlmMessage { Role = "user", Content = user }
            ],
            Tools = [SubmitQuizTool()]
        };
    }

    public GlmChatRequest BuildGradingRequest(QuizQuestion question, string answerText)
    {
        string system =
            "You grade a learner's short answer against the expected answer. " +
            "Award full points only when the answer is substantially correct; partial credit is allowed. " +
            "Call grade_answer exactly once.";

        string user =
            $"Question: {question.Prompt}\nExpected answer: {question.ExpectedAnswer}\n" +
            $"Maximum points: {question.Points}\nLearner answer: {answerText}";

        return new GlmChatRequest
        {
            Model = _model,
            Temperature = 0,
            Messages =
            [
                new GlmMessage { Role = "system", Content = system },
                new GlmMessage { Role = "user", Content = user }
            ],
            Tools = [GradeAnswerTool()]
        };
    }

    #endregion

    #region Private methods region

    private static string BuildContext(IReadOnlyList<RetrievedChunk> chunks)
    {
        var builder = new StringBuilder();
        foreach (RetrievedChunk chunk in chunks)
        {
            if (builder.Length + chunk.Content.Length > MaxContextChars)
            {
                break;
            }

            builder.AppendLine(chunk.Content).AppendLine();
        }

        return builder.ToString().Trim();
    }

    private static GlmToolDefinition SubmitQuizTool() => new()
    {
        Function = new GlmFunctionDefinition
        {
            Name = "submit_quiz",
            Description = "Submit the generated quiz questions grounded in the provided context.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    questions = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                type = new { type = "string", @enum = new[] { "multiple_choice", "short_answer" } },
                                prompt = new { type = "string" },
                                options = new { type = "array", items = new { type = "string" } },
                                correctOptionIndex = new { type = "integer" },
                                expectedAnswer = new { type = "string" }
                            },
                            required = new[] { "type", "prompt" }
                        }
                    }
                },
                required = new[] { "questions" }
            }
        }
    };

    private static GlmToolDefinition GradeAnswerTool() => new()
    {
        Function = new GlmFunctionDefinition
        {
            Name = "grade_answer",
            Description = "Grade the learner's short answer.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    isCorrect = new { type = "boolean" },
                    points = new { type = "integer" },
                    feedback = new { type = "string" }
                },
                required = new[] { "isCorrect", "points", "feedback" }
            }
        }
    };

    #endregion
}
