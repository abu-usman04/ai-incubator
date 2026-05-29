namespace AiIncubator.Server.Models.RequestModels.Query;

public class QueryRequest
{
    public string Question { get; init; } = string.Empty;
    public int TopK { get; init; }
}
