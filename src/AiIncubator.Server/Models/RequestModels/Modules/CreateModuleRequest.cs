namespace AiIncubator.Server.Models.RequestModels.Modules;

public class CreateModuleRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
