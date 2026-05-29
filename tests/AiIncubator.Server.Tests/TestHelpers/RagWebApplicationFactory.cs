namespace AiIncubator.Server.Tests.TestHelpers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using AiIncubator.Server.Services.Ingestion;
using AiIncubator.Server.Services.Retrieval;

public class RagWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IIngestionService> Ingestion { get; } = new();

    public Mock<IQueryService> Query { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Embedding:ServerUrl"] = "http://test.bge/",
                ["Embedding:VectorSize"] = "3",
                ["Chat:BaseUrl"] = "http://test.glm/",
                ["Chat:ApiKey"] = "test-key",
                ["Qdrant:Host"] = "localhost",
                ["Qdrant:Port"] = "6334",
                ["Rag:ChunkSize"] = "100",
                ["Rag:ChunkOverlap"] = "10",
                ["Rag:DefaultTopK"] = "4"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton(Ingestion.Object));
            services.Replace(ServiceDescriptor.Singleton(Query.Object));
        });
    }
}
