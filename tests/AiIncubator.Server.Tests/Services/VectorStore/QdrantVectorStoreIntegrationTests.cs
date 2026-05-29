namespace AiIncubator.Server.Tests.Services.VectorStore;

using FluentAssertions;
using Microsoft.Extensions.Options;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.VectorStore;
using Qdrant.Client;

/// <summary>
/// Integration tests against a real Qdrant instance.
/// </summary>
/// <remarks>
/// Set QDRANT_HOST (and optionally QDRANT_PORT) to enable. Without those
/// env vars these tests no-op so CI does not require Docker.
/// </remarks>
public class QdrantVectorStoreIntegrationTests
{
    private static string? Host => Environment.GetEnvironmentVariable("QDRANT_HOST");

    private static int Port => int.TryParse(Environment.GetEnvironmentVariable("QDRANT_PORT"), out int p) ? p : 6334;

    [Fact]
    public async Task RoundTrip_EnsureCollection_Upsert_Search()
    {
        if (Host is null)
        {
            return;
        }

        string collection = $"ai-incubator-it-{Guid.NewGuid():N}";
        var client = new QdrantClient(Host, Port);
        IOptions<QdrantOptions> options = Options.Create(new QdrantOptions
        {
            Host = Host,
            Port = Port,
            CollectionName = collection
        });
        var store = new QdrantVectorStore(client, options);

        try
        {
            await store.EnsureCollectionAsync(vectorSize: 3, CancellationToken.None);

            var record = new VectorRecord(
                new RagDocument(
                    id: "doc-1#0",
                    content: "the quick brown fox",
                    metadata: new Dictionary<string, string> { ["source"] = "test" }),
                vector: new float[] { 1f, 0f, 0f });

            await store.UpsertAsync(new[] { record }, CancellationToken.None);

            IReadOnlyList<RetrievedChunk> hits = await store.SearchAsync(
                new float[] { 1f, 0f, 0f }, topK: 1, CancellationToken.None);

            hits.Should().HaveCount(1);
            hits[0].DocumentId.Should().Be("doc-1#0");
            hits[0].Content.Should().Be("the quick brown fox");
            hits[0].Metadata.Should().ContainKey("source").WhoseValue.Should().Be("test");
        }
        finally
        {
            await client.DeleteCollectionAsync(collection);
        }
    }
}
