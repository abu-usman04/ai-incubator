namespace AiIncubator.Server.Common;

public class ApiError(string code, string message)
{
    public string Code { get; init; } = code;
    public string Message { get; init; } = message;
}
