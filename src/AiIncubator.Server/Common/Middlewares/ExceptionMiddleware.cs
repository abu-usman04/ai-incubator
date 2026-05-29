using System.Net;
using System.Net.Mime;
using System.Text.Json;
using AiIncubator.Server.Common.Exceptions;

namespace AiIncubator.Server.Common.Middlewares;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    #region Fields

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    #endregion

    #region Public methods

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex, "Application error {Code}: {Message}", ex.ErrorCode, ex.Message);
            await WriteAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "Internal server error");
        }
    }

    #endregion

    #region Private methods

    private static async Task WriteAsync(HttpContext context, HttpStatusCode statusCode, string code, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        ApiResponse<object> payload = ApiResponse<object>.Fail(new ApiError(code, message));
        await JsonSerializer.SerializeAsync(context.Response.Body, payload, JsonOptions);
    }

    #endregion
}
