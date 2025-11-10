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
            logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
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
}
