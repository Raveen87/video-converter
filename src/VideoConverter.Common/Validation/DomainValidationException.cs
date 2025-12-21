using System.Net;

using VideoConverter.Common.Errors;

namespace VideoConverter.Common.Validation;

/// <summary>
/// Custom exception for validation errors that provides field-specific error information.
/// </summary>
public class DomainValidationException(
    string errorMessage,
    string userFriendlyMessage,
    IDictionary<string, string[]>? fieldErrors = null,
    Exception? innerException = null,
    ErrorCode errorCode = ErrorCode.ValidationFailed) : Exception(errorMessage, innerException)
{
    /// <summary>
    /// Gets the field-specific validation errors.
    /// </summary>
    public IDictionary<string, string[]>? FieldErrors { get; } = fieldErrors;

    /// <summary>
    /// Gets the user-friendly message that can be displayed to end users.
    /// </summary>
    public string UserFriendlyMessage { get; } = userFriendlyMessage;

    /// <summary>
    /// Gets the error code associated with the validation error.
    /// </summary>
    public ErrorCode ErrorCode { get; } = errorCode;

    /// <summary>
    /// Converts the validation error to a payload.
    /// </summary>
    /// <returns>A payload containing the error information.</returns>
    public ValidationErrorPayload ToPayload()
    {
        return new ValidationErrorPayload
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            ErrorCode = ErrorCode.ToString(),
            ErrorMessage = Message,
            UserFriendlyMessage = UserFriendlyMessage,
            Timestamp = DateTime.UtcNow,
            FieldErrors = FieldErrors,
        };
    }

    /// <summary>
    /// Converts the validation error to a dictionary for JSON serialization.
    /// </summary>
    /// <returns>A dictionary containing the error information.</returns>
    public IDictionary<string, object> ToDictionary()
    {
        return ToPayload().ToDictionary();
    }
}
