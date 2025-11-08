using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

using VideoConverter.Web.Features.Metadata.Dtos;

namespace VideoConverter.Web.Features.Metadata.Services;

public class TmdbSearchService(
    ILogger<TmdbSearchService> logger,
    HttpClient httpClient,
    IConfiguration configuration,
    Func<TimeSpan, CancellationToken, Task>? delayAsync = null) : ITmdbSearchService
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan MaxCumulativeWait = TimeSpan.FromSeconds(60);
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync = delayAsync ?? Task.Delay;
    private readonly string _apiKey = configuration["TMDB:API_KEY"]
        ?? configuration["Tmdb:ApiKey"]
        ?? throw new InvalidOperationException("TMDb API key not configured");

    public async Task<IReadOnlyList<MovieSearchItemDto>> SearchMoviesAsync(string q, int? year, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Array.Empty<MovieSearchItemDto>();
        }

        try
        {
            var query = Uri.EscapeDataString(q);
            var yearParam = year is not null ? $"&year={year.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/movie?query={query}{yearParam}";

            logger.LogDebug("TMDb movie search: q={Query} year={Year}", q, year);

            var json = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var result = TmdbJson.DeserializeMovieSearchResult(json);
            return result?.Results is null || result.Results.Count is 0
                ? Array.Empty<MovieSearchItemDto>()
                : result.Results
                .Select(m => new MovieSearchItemDto(
                    m.Title,
                    ExtractYear(m.ReleaseDate),
                    m.Overview,
                    GetPosterUrl(m.PosterPath),
                    "tmdb",
                    m.Id))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TMDb movie search failed for q={Query}", q);
            return Array.Empty<MovieSearchItemDto>();
        }
    }

    public async Task<IReadOnlyList<TvShowSearchItemDto>> SearchTvAsync(string q, int? firstAirYear, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Array.Empty<TvShowSearchItemDto>();
        }

        try
        {
            var query = Uri.EscapeDataString(q);
            var yearParam = firstAirYear is not null ? $"&first_air_date_year={firstAirYear.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/tv?query={query}{yearParam}";

            logger.LogDebug("TMDb TV search: q={Query} firstAirYear={Year}", q, firstAirYear);

            var json = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var result = TmdbJson.DeserializeTvSearchResult(json);
            return result?.Results is null || result.Results.Count is 0
                ? Array.Empty<TvShowSearchItemDto>()
                : result.Results
                .Select(s => new TvShowSearchItemDto(
                    s.Name,
                    ExtractYear(s.FirstAirDate),
                    s.Overview,
                    GetPosterUrl(s.PosterPath),
                    "tmdb",
                    s.Id))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TMDb TV search failed for q={Query}", q);
            return Array.Empty<TvShowSearchItemDto>();
        }
    }

    private static int ExtractYear(string? dateString)
    {
        return string.IsNullOrWhiteSpace(dateString) || !DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? 0
            : date.Year;
    }

    private static string? GetPosterUrl(string? posterPath)
    {
        return string.IsNullOrWhiteSpace(posterPath) ? null : $"https://image.tmdb.org/t/p/w500{posterPath}";
    }

    private static TimeSpan ComputeRetryDelay(HttpResponseMessage response, int attempt, TimeSpan remainingBudget)
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

    private async Task<string> GetAuthorizedStringAsync(string url, CancellationToken cancellationToken)
    {
        var totalWait = TimeSpan.Zero;
        HttpStatusCode? lastStatus = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

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
                    logger.LogDebug("TMDb returned {Status}. Retrying in {Delay} (attempt {Attempt}/{MaxRetries})", (int)status, wait, attempt + 1, MaxRetries);
                    totalWait += wait;
                    await _delayAsync(wait, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.LogDebug("TMDb returned {Status}. Retrying immediately (attempt {Attempt}/{MaxRetries})", (int)status, attempt + 1, MaxRetries);
                }

                continue;
            }

            _ = response.EnsureSuccessStatusCode();
        }

        throw new HttpRequestException($"TMDb request failed after {MaxRetries} retries", inner: null, statusCode: lastStatus);
    }
}
