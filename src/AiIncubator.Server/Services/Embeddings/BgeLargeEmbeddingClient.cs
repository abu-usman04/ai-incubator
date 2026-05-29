using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Bge;

namespace AiIncubator.Server.Services.Embeddings;

/// <summary>
/// HTTP client for the internal BGE-Large embedding server.
/// </summary>
/// <remarks>
/// Assumes the server exposes POST {ServerUrl}/embed accepting
/// { "sentences": ["..."] } and returning { "dense": [[...], ...] }.
/// If the real contract differs, only this class and its DTOs change.
/// </remarks>
public class BgeLargeEmbeddingClient(HttpClient httpClient, IOptions<EmbeddingOptions> options) : IEmbeddingClient
{
    #region Private fields region 

    private const string EmbedPath = "embed";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly EmbeddingOptions _options = options.Value;

    #endregion

    #region Public methods region

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        IReadOnlyList<float[]> embeddings = await EmbedBatchAsync([text], cancellationToken);
        if (embeddings.Count == 0)
        {
            throw new AppException(
                HttpStatusCode.InternalServerError,
                "Embedding server returned no vectors for the input.",
                "INTERNAL_ERROR");
        }

        return embeddings[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        if (texts.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        var request = new BgeLargeRequest(texts);
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(EmbedPath, request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new AppException(
                HttpStatusCode.InternalServerError,
                $"Embedding server request failed: {ex.Message}",
                "INTERNAL_ERROR",
                ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AppException(
                HttpStatusCode.InternalServerError,
                $"Embedding server returned {(int)response.StatusCode}: {Truncate(body)}",
                "INTERNAL_ERROR");
        }

        BgeLargeResponse? payload = await response.Content.ReadFromJsonAsync<BgeLargeResponse>(JsonOptions, cancellationToken);
        if (payload is null)
        {
            throw new AppException(
                HttpStatusCode.InternalServerError,
                "Embedding server returned an empty body.",
                "INTERNAL_ERROR");
        }

        ValidateVectorSize(payload.Dense);
        return payload.Dense;
    }

    #endregion

    #region Private methods region

    private void ValidateVectorSize(IReadOnlyList<float[]> embeddings)
    {
        foreach (float[] vector in embeddings)
        {
            if (vector.Length != _options.VectorSize)
            {
                throw new AppException(
                    HttpStatusCode.InternalServerError,
                    $"Embedding server returned vector of length {vector.Length}, expected {_options.VectorSize}.",
                    "INTERNAL_ERROR");
            }
        }
    }

    private static string Truncate(string value)
    {
        const int MaxLength = 200;
        return value.Length <= MaxLength ? value : value[..MaxLength];
    }

    #endregion
}
