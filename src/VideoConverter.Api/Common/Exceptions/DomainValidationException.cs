using System.Net;

namespace VideoConverter.Api.Common.Exceptions;

/// <summary>
/// Custom exception for validation errors that provides field-specific error information.
/// </summary>
public class DomainValidationException(
    string errorMessage,
    string userFriendlyMessage,
    IDictionary<string, string[]>? fieldErrors = null,
    Exception? innerException = null) : Exception(errorMessage, innerException)
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
    /// Converts the validation error to a dictionary for JSON serialization.
    /// </summary>
    /// <returns>A dictionary containing the error information.</returns>
    public IDictionary<string, object> ToDictionary()
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            { "statusCode", (int)HttpStatusCode.BadRequest },
            { "errorCode", nameof(ErrorCode.ValidationFailed) },
            { "errorMessage", Message },
            { "userFriendlyMessage", UserFriendlyMessage },
            { "timestamp", DateTime.UtcNow },
        };

        if (FieldErrors is not null && FieldErrors.Count > 0)
        {
            result["fieldErrors"] = FieldErrors;
        }

        return result;
    }
}
