namespace AiIncubator.Server.Tests.Common.Middlewares;

using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Common.Middlewares;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task PassesThrough_WhenNoExceptionThrown()
    {
        HttpContext context = CreateContext();
        var middleware = new ExceptionMiddleware(_ => Task.CompletedTask, NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task MapsAppException_ToConfiguredStatusAndPayload()
    {
        HttpContext context = CreateContext();
        var middleware = new ExceptionMiddleware(
            _ => throw new AppException(HttpStatusCode.BadRequest, "bad input", "BAD_REQUEST"),
            NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().StartWith(MediaTypeNames.Application.Json);

        ApiResponse<object>? payload = ReadResponse<ApiResponse<object>>(context);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        payload.Error.Should().NotBeNull();
        payload.Error!.Code.Should().Be("BAD_REQUEST");
        payload.Error.Message.Should().Be("bad input");
    }

    [Fact]
    public async Task MapsUnknownException_To500WithInternalErrorCode()
    {
        HttpContext context = CreateContext();
        var middleware = new ExceptionMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        ApiResponse<object>? payload = ReadResponse<ApiResponse<object>>(context);
        payload!.Error!.Code.Should().Be("INTERNAL_ERROR");
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static T? ReadResponse<T>(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
