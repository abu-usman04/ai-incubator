using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Helpers.Configurations;

namespace AiIncubator.Server.Services.Ingestion;

public class CharacterTextSplitter : ITextSplitter
{
    #region Fields

    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    #endregion

    #region Constructors

    public CharacterTextSplitter(IOptions<RagOptions> options)
    {
        _chunkSize = options.Value.ChunkSize;
        _chunkOverlap = options.Value.ChunkOverlap;

        if (_chunkSize <= 0)
        {
            throw new ArgumentException($"{nameof(RagOptions.ChunkSize)} must be positive.", nameof(options));
        }

        if (_chunkOverlap < 0 || _chunkOverlap >= _chunkSize)
        {
            throw new ArgumentException(
                $"{nameof(RagOptions.ChunkOverlap)} must be in [0, {nameof(RagOptions.ChunkSize)}).",
                nameof(options));
        }
    }

    #endregion

    #region Public methods

    public IReadOnlyList<string> Split(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        int stride = _chunkSize - _chunkOverlap;
        var chunks = new List<string>();
        int cursor = 0;
        while (cursor < text.Length)
        {
            int end = Math.Min(cursor + _chunkSize, text.Length);
            chunks.Add(text[cursor..end]);
            if (end == text.Length)
            {
                break;
            }

            cursor += stride;
        }

        return chunks;
    }

    #endregion
}
