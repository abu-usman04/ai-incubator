using System.Net;

namespace AiIncubator.Server.Common.Exceptions;

public class AppException : Exception
{
    public AppException(HttpStatusCode statusCode, string message, string errorCode)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public AppException(HttpStatusCode statusCode, string message, string errorCode, Exception inner)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string ErrorCode { get; }
}
