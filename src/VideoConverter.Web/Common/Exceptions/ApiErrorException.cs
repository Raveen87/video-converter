using System.Net;

namespace VideoConverter.Web.Common.Exceptions;

/// <summary>
/// Custom exception for API errors that provides structured error information.
/// </summary>
public class ApiErrorException(
    HttpStatusCode statusCode,
    ErrorCode errorCode,
    string errorMessage,
    string userFriendlyMessage,
    Exception? innerException = null) : Exception(errorMessage, innerException)
{
    /// <summary>
    /// Gets the HTTP status code for this error.
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>
    /// Gets the error code that can be used programmatically to identify the specific error.
    /// </summary>
    public ErrorCode ErrorCode { get; } = errorCode;

    /// <summary>
    /// Gets the error message meant for developers/employees.
    /// </summary>
    public string ErrorMessage { get; } = errorMessage;

    /// <summary>
    /// Gets the user-friendly message that can be displayed to end users.
    /// </summary>
    public string UserFriendlyMessage { get; } = userFriendlyMessage;

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="errorMessage">The error message for developers.</param>
    /// <param name="userFriendlyMessage">The user-friendly message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <returns>A new ApiErrorException instance for validation errors.</returns>
    public static ApiErrorException ValidationError(string errorMessage, string userFriendlyMessage, Exception? innerException = null)
    {
        return new ApiErrorException(HttpStatusCode.BadRequest, ErrorCode.ValidationFailed, errorMessage, userFriendlyMessage, innerException);
    }

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="errorCode">The specific error code for the not found scenario.</param>
    /// <param name="errorMessage">The error message for developers.</param>
    /// <param name="userFriendlyMessage">The user-friendly message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <returns>A new ApiErrorException instance for not found errors.</returns>
    public static ApiErrorException NotFound(ErrorCode errorCode, string errorMessage, string userFriendlyMessage, Exception? innerException = null)
    {
        return new ApiErrorException(HttpStatusCode.NotFound, errorCode, errorMessage, userFriendlyMessage, innerException);
    }

    /// <summary>
    /// Creates a business logic error.
    /// </summary>
    /// <param name="errorCode">The specific error code for the business logic error.</param>
    /// <param name="errorMessage">The error message for developers.</param>
    /// <param name="userFriendlyMessage">The user-friendly message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <returns>A new ApiErrorException instance for business logic errors.</returns>
    public static ApiErrorException BusinessError(ErrorCode errorCode, string errorMessage, string userFriendlyMessage, Exception? innerException = null)
    {
        return new ApiErrorException(HttpStatusCode.BadRequest, errorCode, errorMessage, userFriendlyMessage, innerException);
    }

    /// <summary>
    /// Creates a server error.
    /// </summary>
    /// <param name="errorCode">The specific error code for the server error.</param>
    /// <param name="errorMessage">The error message for developers.</param>
    /// <param name="userFriendlyMessage">The user-friendly message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <returns>A new ApiErrorException instance for server errors.</returns>
    public static ApiErrorException ServerError(ErrorCode errorCode, string errorMessage, string userFriendlyMessage, Exception? innerException = null)
    {
        return new ApiErrorException(HttpStatusCode.InternalServerError, errorCode, errorMessage, userFriendlyMessage, innerException);
    }

    /// <summary>
    /// Converts the ApiError to a dictionary for JSON serialization.
    /// </summary>
    /// <returns>A dictionary containing the error information.</returns>
    public IDictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            { "statusCode", (int)StatusCode },
            { "errorCode", ErrorCode.ToString() },
            { "errorMessage", ErrorMessage },
            { "userFriendlyMessage", UserFriendlyMessage },
            { "timestamp", DateTime.UtcNow },
        };
    }
}
