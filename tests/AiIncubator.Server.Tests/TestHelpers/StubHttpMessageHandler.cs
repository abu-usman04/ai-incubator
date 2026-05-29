namespace AiIncubator.Server.Tests.TestHelpers;

public class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync();
        }

        Requests.Add(request);
        return await handler(request);
    }
}
