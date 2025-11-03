using System.Net;

namespace VideoConverter.Web.Common.Exceptions;

public sealed class ApiErrorMapper : IApiErrorMapper
{
    public object Map(Exception exception)
    {
        return exception switch
        {
            ApiErrorException apiException => apiException,
            DomainValidationException validationEx => validationEx,
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
                exception),
        };
    }

    public int GetStatusCode(object apiError)
    {
        return apiError switch
        {
            ApiErrorException apiException => (int)apiException.StatusCode,
            DomainValidationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError,
        };
    }

    public IDictionary<string, object?> ToResponse(object apiError)
    {
        return apiError switch
        {
            ApiErrorException apiException => apiException.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.Ordinal),
            DomainValidationException validationException => validationException.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.Ordinal),
            _ => throw new InvalidOperationException("Unexpected error type"),
        };
    }
}
