namespace VideoConverter.Web.Shared.Validation;

public static class ValidationResultExtensions
{
    public static void AddIfInvalid(this ValidationResult result,
        bool isValid,
        string fieldName,
        string? errorMessage,
        ILogger logger,
        object? attemptedValue = null)
    {
        if (isValid)
        {
            return;
        }

        if (attemptedValue is null)
        {
            logger.LogWarning("Validation failed: {Field} - {Error}", fieldName, errorMessage);
        }
        else
        {
            logger.LogWarning("Validation failed: {Field} value {Value} - {Error}", fieldName, attemptedValue, errorMessage);
        }

        result.Add(fieldName, errorMessage!);
    }
}
