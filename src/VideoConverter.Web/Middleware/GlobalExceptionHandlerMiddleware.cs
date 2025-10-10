using System.Net;
using System.Text.Json;
using VideoConverter.Web.Exceptions;

namespace VideoConverter.Web.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and converting them to structured API responses.
/// </summary>
public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var apiError = exception switch
        {
            ApiErrorException apiException => apiException,
            ArgumentNullException nullEx => ApiErrorException.ValidationError(
                nullEx.Message,
                "A required parameter is missing. Please provide all required information.",
                nullEx),
            ArgumentException argEx => ApiErrorException.ValidationError(
                argEx.Message,
                "The provided input is invalid. Please check your request and try again.",
                argEx),
            InvalidOperationException opEx => ApiErrorException.BusinessError(
                ErrorCode.InvalidConversionState,
                opEx.Message,
                "The requested operation cannot be completed in the current state.",
                opEx),
            FileNotFoundException fileEx => ApiErrorException.NotFound(
                ErrorCode.FileNotFound,
                fileEx.Message,
                "The requested file could not be found.",
                fileEx),
            DirectoryNotFoundException dirEx => ApiErrorException.NotFound(
                ErrorCode.FileNotFound,
                dirEx.Message,
                "The requested directory could not be found.",
                dirEx),
            UnauthorizedAccessException authEx => new ApiErrorException(
                HttpStatusCode.Forbidden,
                ErrorCode.InternalServerError,
                authEx.Message,
                "You do not have permission to access this resource.",
                authEx),
            TimeoutException timeoutEx => ApiErrorException.ServerError(
                ErrorCode.InternalServerError,
                timeoutEx.Message,
                "The operation timed out. Please try again later.",
                timeoutEx),
            _ => ApiErrorException.ServerError(
                ErrorCode.InternalServerError,
                "An unexpected error occurred.",
                "An unexpected error occurred. Please try again later.",
                exception)
        };

        response.StatusCode = (int)apiError.StatusCode;

        var errorResponse = apiError.ToDictionary();
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await response.WriteAsync(jsonResponse);
    }
}
