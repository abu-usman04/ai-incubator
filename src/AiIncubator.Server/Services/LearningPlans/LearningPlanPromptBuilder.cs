using System.Text;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.Glm;

namespace AiIncubator.Server.Services.LearningPlans;

/// <summary>
/// Builds the GLM tool-calling request for the planning agent. The agent is given the goal and a
/// catalog of available modules and documents, and must return a structured plan via
/// <c>submit_learning_plan</c> referencing real catalog ids.
/// </summary>
public class LearningPlanPromptBuilder(IOptions<ChatOptions> chatOptions)
{
    #region Private fields region

    private readonly string _model = chatOptions.Value.Model;

    #endregion

    #region Public methods region

    public GlmChatRequest BuildPlanRequest(
        string goal,
        int weekCount,
        IReadOnlyList<KnowledgeModule> modules,
        IReadOnlyList<Document> documents)
    {
        string catalog = BuildCatalog(modules, documents);

        string system =
            "You are a learning planner. Build a multi-week study plan that reaches the learner's GOAL " +
            "using ONLY the available modules and documents in the CATALOG. " +
            "Each task has a type: 'read' (study a document/module), 'quiz' (test a topic), or 'chat' " +
            "(ask the assistant about a module). Put the relevant catalog id in referenceId when applicable. " +
            "Call submit_learning_plan exactly once.";

        string user =
            $"GOAL: {goal}\nWeeks: {weekCount}\n\nCATALOG:\n{catalog}";

        return new GlmChatRequest
        {
            Model = _model,
            Temperature = 0.3,
            Messages =
            [
                new GlmMessage { Role = "system", Content = system },
                new GlmMessage { Role = "user", Content = user }
            ],
            Tools = [SubmitPlanTool()]
        };
    }

    #endregion

    #region Private methods region

    private static string BuildCatalog(IReadOnlyList<KnowledgeModule> modules, IReadOnlyList<Document> documents)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Modules:");
        foreach (KnowledgeModule module in modules)
        {
            builder.AppendLine($"- id={module.Id} name=\"{module.Name}\" description=\"{module.Description}\"");
        }

        builder.AppendLine("Documents:");
        foreach (Document document in documents)
        {
            builder.AppendLine($"- id={document.Id} fileName=\"{document.FileName}\"");
        }

        return builder.ToString().Trim();
    }

    private static GlmToolDefinition SubmitPlanTool() => new()
    {
        Function = new GlmFunctionDefinition
        {
            Name = "submit_learning_plan",
            Description = "Submit the structured multi-week learning plan grounded in the catalog.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    summary = new { type = "string" },
                    weeks = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                weekNumber = new { type = "integer" },
                                theme = new { type = "string" },
                                tasks = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            type = new { type = "string", @enum = new[] { "read", "quiz", "chat" } },
                                            title = new { type = "string" },
                                            description = new { type = "string" },
                                            referenceId = new { type = "string" }
                                        },
                                        required = new[] { "type", "title" }
                                    }
                                }
                            },
                            required = new[] { "weekNumber", "theme", "tasks" }
                        }
                    }
                },
                required = new[] { "weeks" }
            }
        }
    };

    #endregion
}
