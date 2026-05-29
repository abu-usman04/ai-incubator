namespace AiIncubator.Server.Models.Domain;

public class RagAnswer(string answer, IReadOnlyList<RetrievedChunk> sources)
{
    public string Answer { get; init; } = answer;

    public IReadOnlyList<RetrievedChunk> Sources { get; init; } = sources;
}
