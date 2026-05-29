using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AiIncubator.Server.Services.VectorStore;

public class QdrantVectorStore(
    QdrantClient client, IOptions<QdrantOptions> options) : IVectorStore
{
    #region Private fields region

    private const string ContentPayloadKey = "content";
    private const string DocumentIdPayloadKey = "document_id";
    private const string SourceDocumentIdPayloadKey = "source_document_id";
    private readonly string _collection = options.Value.CollectionName;

    #endregion

    #region Public methods region

    
    //todo the function below almost does not give a value, think what actually the ensure collection async should do.
    public async Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken)
    {
        var isExist = await client.CollectionExistsAsync(_collection, cancellationToken);
        if (!isExist)
        {
            await client.CreateCollectionAsync(
                _collection,
                new VectorParams
                {
                    Size = (ulong)vectorSize,
                    // ------------ Attention ------------
                    // Distance metric used for vector similarity search:
                    // Cosine   --> compares vector direction (best for text embeddings / semantic search)
                    // ------------ Attention ------------
                    Distance = Distance.Cosine
                },
                cancellationToken: cancellationToken);
            return;
        }

        CollectionInfo info = await client.GetCollectionInfoAsync(_collection, cancellationToken);
        ulong existingSize = info.Config.Params.VectorsConfig.Params.Size;
        if (existingSize != (ulong)vectorSize)
        {
            throw new AppException(
                HttpStatusCode.InternalServerError,
                $"Qdrant collection '{_collection}' exists with vector size {existingSize}, expected {vectorSize}.",
                errorCode: "INTERNAL_ERROR");
        }
    }

    public async Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken)
    {
        if (records.Count == 0)
        {
            return;
        }

        var points = new List<PointStruct>(records.Count);
        foreach (VectorRecord record in records)
        {
            points.Add(ToPoint(record));
        }

        await client.UpsertAsync(_collection, points, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        IReadOnlyList<float> queryVector,
        int topK,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ScoredPoint> results = await client.SearchAsync(
            _collection,
            queryVector.ToArray(),
            limit: (ulong)topK,
            payloadSelector: new WithPayloadSelector { Enable = true },
            cancellationToken: cancellationToken);

        var chunks = new List<RetrievedChunk>(results.Count);
        foreach (ScoredPoint point in results)
        {
            chunks.Add(MapToChunk(point));
        }

        return chunks;
    }

    public async Task DeleteByDocumentAsync(string sourceDocumentId, CancellationToken cancellationToken)
    {
        if (!await client.CollectionExistsAsync(_collection, cancellationToken))
        {
            return;
        }

        var filter = new Filter
        {
            Must =
            {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = SourceDocumentIdPayloadKey,
                        Match = new Match { Keyword = sourceDocumentId }
                    }
                }
            }
        };

        await client.DeleteAsync(_collection, filter, cancellationToken: cancellationToken);
    }

    #endregion

    #region Private methods region

    private static PointStruct ToPoint(VectorRecord record)
    {
        var point = new PointStruct
        {
            Id = new PointId
            {
                Uuid = ToDeterministicUuid(record.Document.Id)
            },
            Vectors = record.Vector.ToArray(),
            Payload =
            {
                [DocumentIdPayloadKey] = record.Document.Id,
                [ContentPayloadKey] = record.Document.Content
            }
        };
        foreach (KeyValuePair<string, string> meta in record.Document.Metadata)
        {
            if (meta.Key == DocumentIdPayloadKey || meta.Key == ContentPayloadKey)
            {
                continue;
            }

            point.Payload[meta.Key] = meta.Value;
        }

        return point;
    }

    private static RetrievedChunk MapToChunk(ScoredPoint point)
    {
        string documentId = ReadString(point.Payload, DocumentIdPayloadKey) ?? string.Empty;
        string content = ReadString(point.Payload, ContentPayloadKey) ?? string.Empty;
        var metadata = new Dictionary<string, string>();
        foreach (KeyValuePair<string, Value> kv in point.Payload)
        {
            if (kv.Key == DocumentIdPayloadKey || kv.Key == ContentPayloadKey)
            {
                continue;
            }

            if (kv.Value.KindCase == Value.KindOneofCase.StringValue)
            {
                metadata[kv.Key] = kv.Value.StringValue;
            }
        }

        return new RetrievedChunk(documentId, content, point.Score, metadata);
    }

    private static string? ReadString(IDictionary<string, Value> payload, string key)
    {
        return payload.TryGetValue(key, out Value? value) && value.KindCase == Value.KindOneofCase.StringValue
            ? value.StringValue
            : null;
    }

    private static string ToDeterministicUuid(string input)
    {
        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(Encoding.UTF8.GetBytes(input), hash);

        Span<byte> guid = stackalloc byte[16];
        hash[..16].CopyTo(guid);
        guid[6] = (byte)((guid[6] & 0x0F) | 0x50);
        guid[8] = (byte)((guid[8] & 0x3F) | 0x80);

        return new Guid(guid).ToString();
    }

    #endregion
}