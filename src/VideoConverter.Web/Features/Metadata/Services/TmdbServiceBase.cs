using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace VideoConverter.Web.Features.Metadata.Services;

public abstract class TmdbServiceBase(
    ILogger logger,
    HttpClient httpClient,
    IConfiguration configuration,
    Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan MaxCumulativeWait = TimeSpan.FromSeconds(60);
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync = delayAsync ?? Task.Delay;
    private readonly string _apiKey = configuration["TMDB:API_KEY"]
        ?? configuration["Tmdb:ApiKey"]
        ?? throw new InvalidOperationException("TMDb API key not configured");

    protected ILogger Logger { get; } = logger;

    protected HttpClient HttpClient { get; } = httpClient;

    protected static int ExtractYear(string? dateString)
    {
        return string.IsNullOrWhiteSpace(dateString) || !DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? 0
            : date.Year;
    }

    protected static string? GetPosterUrl(string? posterPath)
    {
        return string.IsNullOrWhiteSpace(posterPath) ? null : $"https://image.tmdb.org/t/p/w500{posterPath}";
    }

    protected static TimeSpan ComputeRetryDelay(HttpResponseMessage response, int attempt, TimeSpan remainingBudget)
    {
        if (remainingBudget <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is not null)
        {
            var wait = retryAfter.Delta;
            if (wait is null && retryAfter.Date is not null)
            {
                var now = DateTimeOffset.UtcNow;
                var target = retryAfter.Date.Value;
                wait = target - now;
            }

            if (wait is not null)
            {
                var clamped = wait.Value < TimeSpan.Zero ? TimeSpan.Zero : wait.Value;
                return clamped <= remainingBudget ? clamped : remainingBudget;
            }
        }

        var baseDelaySeconds = Math.Pow(2, Math.Min(attempt, MaxRetries));
        var baseDelay = TimeSpan.FromSeconds(baseDelaySeconds);
        var jitterMillis = (int)Math.Min(Math.Ceiling(baseDelay.TotalMilliseconds * Random.Shared.NextDouble()), int.MaxValue);
        var jittered = TimeSpan.FromMilliseconds(jitterMillis);
        return jittered <= remainingBudget ? jittered : remainingBudget;
    }

    protected async Task<string> GetAuthorizedStringAsync(string url, CancellationToken cancellationToken)
    {
        var totalWait = TimeSpan.Zero;
        HttpStatusCode? lastStatus = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var response = await HttpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            var status = response.StatusCode;
            lastStatus = status;
            var isRetryable = status is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;
            if (isRetryable && attempt < MaxRetries)
            {
                var remainingBudget = MaxCumulativeWait - totalWait;
                var wait = ComputeRetryDelay(response, attempt, remainingBudget);
                if (wait > TimeSpan.Zero)
                {
                    Logger.LogDebug("TMDb returned {Status}. Retrying in {Delay} (attempt {Attempt}/{MaxRetries})", (int)status, wait, attempt + 1, MaxRetries);
                    totalWait += wait;
                    await _delayAsync(wait, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Logger.LogDebug("TMDb returned {Status}. Retrying immediately (attempt {Attempt}/{MaxRetries})", (int)status, attempt + 1, MaxRetries);
                }

                continue;
            }

            _ = response.EnsureSuccessStatusCode();
        }

        throw new HttpRequestException($"TMDb request failed after {MaxRetries} retries", inner: null, statusCode: lastStatus);
    }
}
