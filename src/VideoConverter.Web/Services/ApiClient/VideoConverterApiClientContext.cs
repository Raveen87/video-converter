namespace VideoConverter.Web.Services.ApiClient;

internal sealed class VideoConverterApiClientContext
{
    public VideoConverterApiClientContext(HttpClient httpClient, Microsoft.Extensions.Logging.ILogger logger)
    {
        HttpClient = httpClient;
        Logger = logger;
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };
    }

    public HttpClient HttpClient { get; }

    public JsonSerializerOptions JsonOptions { get; }

    public Microsoft.Extensions.Logging.ILogger Logger { get; }

    public static async Task<string> ExtractUserFriendlyErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(errorJson))
            {
                return $"Error: {response.StatusCode}";
            }

            System.Diagnostics.Debug.WriteLine($"Error response body: {errorJson}");

            try
            {
                using var doc = JsonDocument.Parse(errorJson);
                if (doc.RootElement.TryGetProperty("userFriendlyMessage", out var userFriendlyMessage))
                {
                    return userFriendlyMessage.GetString() ?? errorJson;
                }

                if (doc.RootElement.TryGetProperty("errorMessage", out var errorMessage))
                {
                    return errorMessage.GetString() ?? errorJson;
                }

                if (doc.RootElement.TryGetProperty("fieldErrors", out var fieldErrors))
                {
                    var fieldErrorsStr = fieldErrors.ToString();
                    return $"Validation errors: {fieldErrorsStr}";
                }

                return errorJson;
            }
            catch (JsonException)
            {
                return errorJson;
            }
        }
        catch (Exception ex)
        {
            return $"Error: {response.StatusCode} - {ex.Message}";
        }
    }

    public async Task EnsureSuccessAsync(HttpResponseMessage response, string actionDescription, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractUserFriendlyErrorMessageAsync(response, cancellationToken).ConfigureAwait(false);
            var statusCode = response.StatusCode;
            Logger.LogError("{Action} failed with status {StatusCode}: {ErrorMessage}", actionDescription, statusCode, errorMessage);
            throw new HttpRequestException($"Error: {statusCode} - {errorMessage}");
        }
    }
}
