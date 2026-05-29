namespace AiIncubator.Server.Common.Helpers.Configurations;

public class DocumentsOptions
{
    public const string SectionName = "Documents";

    public long MaxUploadBytes { get; init; } = 26_214_400;

    public string[] AllowedExtensions { get; init; } = [".txt", ".md"];
}
