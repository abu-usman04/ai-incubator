using System.Net;
using System.Text;
using AiIncubator.Server.Common.Exceptions;

namespace AiIncubator.Server.Services.Documents;

/// <summary>
/// Extracts plain text from uploaded files. Plain text and markdown are decoded directly.
/// PDF and other binary formats are an extension point: add a parser branch here and the
/// corresponding extension to <c>Documents:AllowedExtensions</c>.
/// </summary>
public class DocumentTextExtractor : IDocumentTextExtractor
{
    #region Public methods region

    public async Task<string> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".txt" or ".md" => await ReadTextAsync(content, cancellationToken),
            _ => throw new AppException(
                HttpStatusCode.BadRequest,
                $"Unsupported file type '{extension}'.",
                "BAD_REQUEST")
        };
    }

    #endregion

    #region Private methods region

    private static async Task<string> ReadTextAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    #endregion
}
