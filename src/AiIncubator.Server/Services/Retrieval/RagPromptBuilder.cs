using System.Text;
using AiIncubator.Server.Models.Domain;

namespace AiIncubator.Server.Services.Retrieval;

public class RagPromptBuilder
{
    #region Private fields region

    private const string SystemPromptTemplate =
        "You are a helpful assistant. Answer the user's question using ONLY the context provided below. " +
        "If the context does not contain enough information to answer, reply exactly: \"I don't know.\" " +
        "Do not invent facts. Cite relevant context blocks by their bracketed number when useful.";

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