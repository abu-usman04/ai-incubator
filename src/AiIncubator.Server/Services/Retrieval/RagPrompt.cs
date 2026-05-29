namespace AiIncubator.Server.Services.Retrieval;

public class RagPrompt(string system, string user)
{
    public string System { get; init; } = system;

    public string User { get; init; } = user;
}
