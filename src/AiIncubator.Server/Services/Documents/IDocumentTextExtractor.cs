namespace AiIncubator.Server.Services.Documents;

public interface IDocumentTextExtractor
{
    /// <summary>
    /// Extracts plain text from an uploaded file based on its extension.
    /// Supports .txt, .md (decoded as UTF-8) and .pdf (page text).
    /// </summary>
    Task<string> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken);
}
