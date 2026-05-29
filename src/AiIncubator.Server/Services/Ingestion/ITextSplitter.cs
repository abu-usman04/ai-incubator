namespace AiIncubator.Server.Services.Ingestion;

public interface ITextSplitter
{
    IReadOnlyList<string> Split(string text);
}
