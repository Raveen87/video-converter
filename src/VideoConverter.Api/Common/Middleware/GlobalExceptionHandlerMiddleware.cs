using System.Text.Json;

using VideoConverter.Api.Common.Exceptions;

namespace VideoConverter.Api.Common.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and converting them to structured API responses.
/// </summary>
public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IApiErrorMapper mapper)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogException(context, ex);
            await HandleExceptionAsync(context, ex, mapper).ConfigureAwait(false);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, IApiErrorMapper mapper)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var apiError = mapper.Map(exception);
        var errorResponse = mapper.ToResponse(apiError);
        response.StatusCode = mapper.GetStatusCode(apiError);

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        });

        await response.WriteAsync(jsonResponse, cancellationToken: context.RequestAborted).ConfigureAwait(false);
    }

    private void LogException(HttpContext context, Exception exception)
    {
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var innerException = exception.InnerException;

        logger.LogError(
            exception,
            "An unhandled exception occurred. Method: {Method}, Path: {Path}, Message: {Message}, Type: {ExceptionType}",
            requestMethod,
            requestPath,
            exception.Message,
            exception.GetType().Name);

        if (innerException is not null)
        {
            logger.LogError(
                innerException,
                "Inner exception: {InnerExceptionType}, Message: {InnerExceptionMessage}",
                innerException.GetType().Name,
                innerException.Message);
        }

        if (exception is ApiErrorException apiError && apiError.Details is not null && apiError.Details.Count > 0)
        {
            var errorDetails = JsonSerializer.Serialize(apiError.Details, new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            logger.LogError("Error details:\n{ErrorDetails}", errorDetails);
        }
        else if (exception.Data.Count > 0)
        {
            var errorDetails = JsonSerializer.Serialize(exception.Data, new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            logger.LogError("Error details:\n{ErrorDetails}", errorDetails);
        }
    }
}
