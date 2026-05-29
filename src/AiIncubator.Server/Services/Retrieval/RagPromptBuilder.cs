using System.Text;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Retrieval;

public class RagPromptBuilder
{
    #region Private fields region

    private const string SystemPromptTemplate =
        "You are Ilm AI, a friendly and knowledgeable assistant. Answer the user's question in clear, " +
        "natural language, the way you would explain it to a colleague. Use only the information in the " +
        "context below. Write in complete sentences, and use short paragraphs or bullet points when they " +
        "make the answer easier to read. Do not mention \"the context\" and do not put bracketed reference " +
        "numbers in your answer. If the information needed is not in the context, say you don't know based " +
        "on the available documents. Do not invent facts.";

    #endregion

    #region Public methods region

    public RagPrompt Build(string question, IReadOnlyList<RetrievedChunk> sources)
    {
        var user = new StringBuilder();
        user.AppendLine("Context:");
        if (sources.Count == 0)
        {
            user.AppendLine("(no context retrieved)");
        }
        else
        {
            for (int i = 0; i < sources.Count; i++)
            {
                user.Append('[').Append(i + 1).Append("] ").AppendLine(sources[i].Content);
            }
        }

        user.AppendLine();
        user.Append("Question: ").Append(question);

        return new RagPrompt(SystemPromptTemplate, user.ToString());
    }

    #endregion
}