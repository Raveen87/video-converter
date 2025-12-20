namespace VideoConverter.Common.Validation;

public sealed class ValidationErrorPayload
{
    public int StatusCode { get; init; }

    public string ErrorCode { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public string UserFriendlyMessage { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; }

    public IDictionary<string, string[]>? FieldErrors { get; init; }

    public IDictionary<string, object> ToDictionary()
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            { "statusCode", StatusCode },
            { "errorCode", ErrorCode },
            { "errorMessage", ErrorMessage },
            { "userFriendlyMessage", UserFriendlyMessage },
            { "timestamp", Timestamp },
        };

        if (FieldErrors is not null && FieldErrors.Count > 0)
        {
            result["fieldErrors"] = FieldErrors;
        }

        return result;
    }
}
