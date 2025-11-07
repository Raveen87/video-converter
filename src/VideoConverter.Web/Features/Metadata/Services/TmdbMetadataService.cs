using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public class TmdbMetadataService(
    ILogger<TmdbMetadataService> logger,
    HttpClient httpClient,
    IConfiguration configuration,
    Func<TimeSpan, CancellationToken, Task>? delayAsync = null) : IMetadataService
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan MaxCumulativeWait = TimeSpan.FromSeconds(60);
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync = delayAsync ?? Task.Delay;
    private readonly string _apiKey = configuration["TMDB:API_KEY"]
        ?? configuration["Tmdb:ApiKey"]
        ?? throw new InvalidOperationException("TMDb API key not configured");

    public async Task<MovieMetadata?> GetMovieMetadataAsync(string title, int? year = null, string? source = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(title);
            var yearParam = year is not null ? $"&year={year.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/movie?query={query}{yearParam}";

            logger.LogDebug("Searching for movie: {Title} (Year: {Year})", title, year);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var searchResult = TmdbJson.DeserializeMovieSearchResult(responseString);

            if (searchResult?.Results?.Count > 0)
            {
                var movie = searchResult.Results[0];
                return new MovieMetadata(
                    movie.Title,
                    Helpers.ExtractYear(movie.ReleaseDate),
                    source,
                    movie.Overview,
                    Helpers.GetPosterUrl(movie.PosterPath));
            }

            logger.LogWarning("No movie found for: {Title} (Year: {Year})", title, year);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching movie metadata for: {Title}", title);
            return null;
        }
    }

    public async Task<TvShowMetadata?> GetTvShowMetadataAsync(string showName, int season, int episode, int? firstAiredYear = null, string? source = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(showName);
            var yearParam = firstAiredYear is not null ? $"&first_air_date_year={firstAiredYear.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/tv?query={query}{yearParam}";

            logger.LogDebug("Searching for TV show: {ShowName} (Year: {Year})", showName, firstAiredYear);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var searchResult = TmdbJson.DeserializeTvSearchResult(responseString);

            if (searchResult?.Results?.Count is > 0)
            {
                var show = searchResult.Results[0];

                // Get episode details
                var episodeDetails = await GetEpisodeDetailsAsync(show.Id, season, episode, cancellationToken).ConfigureAwait(false);

                return new TvShowMetadata(
                    show.Name,
                    Helpers.ExtractYear(show.FirstAirDate),
                    season,
                    episode,
                    episodeDetails?.Name,
                    source,
                    episodeDetails?.Overview ?? show.Overview,
                    Helpers.GetPosterUrl(show.PosterPath));
            }

            var fallback = await TryBuildTvShowMetadataFromSearchJsonAsync(responseString, showName, season, episode, source, cancellationToken).ConfigureAwait(false);
            if (fallback is not null)
            {
                return fallback;
            }

            logger.LogWarning("No TV show found for: {ShowName} (Year: {Year})", showName, firstAiredYear);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching TV show metadata for: {ShowName}", showName);
            return null;
        }
    }

    public async Task<TmdbTvShowDetailsResponse?> GetTvShowDetailsAsync(int showId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{showId}";
            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            return TmdbJson.DeserializeTvShowDetails(responseString);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error fetching TV show details for show {ShowId}", showId);
            return null;
        }
    }

    public async Task<TmdbSeasonDetailsResponse?> GetTvSeasonDetailsAsync(int showId, int seasonNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{showId}/season/{seasonNumber}";
            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            return TmdbJson.DeserializeTvSeasonDetails(responseString);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error fetching season details for show {ShowId}, season {Season}", showId, seasonNumber);
            return null;
        }
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

        // Exponential backoff with full jitter. Base 1s * 2^attempt, clamped by remaining budget.
        var baseDelaySeconds = Math.Pow(2, Math.Min(attempt, MaxRetries));
        var baseDelay = TimeSpan.FromSeconds(baseDelaySeconds);
        var jitterMillis = (int)Math.Ceiling(baseDelay.TotalMilliseconds * Random.Shared.NextDouble());
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

            // Will throw with proper HttpRequestException including status code
            _ = response.EnsureSuccessStatusCode();
        }

        throw new HttpRequestException($"TMDb request failed after {MaxRetries} retries", inner: null, statusCode: lastStatus);
    }

    private async Task<TmdbJson.TmdbEpisodeDetails?> GetEpisodeDetailsAsync(int showId, int season, int episode, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{showId}/season/{season}/episode/{episode}";
            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            return TmdbJson.DeserializeEpisodeDetails(responseString);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error fetching episode details for show {ShowId}, season {Season}, episode {Episode}", showId, season, episode);
            return null;
        }
    }

    private async Task<TvShowMetadata?> TryBuildTvShowMetadataFromSearchJsonAsync(string responseString, string showName, int season, int episode, string? source, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("results", out var resultsEl) && resultsEl.ValueKind is System.Text.Json.JsonValueKind.Array)
            {
                var first = resultsEl.EnumerateArray().FirstOrDefault();
                if (first.ValueKind is System.Text.Json.JsonValueKind.Object && first.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var id))
                {
                    var name = first.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : showName;
                    var firstAirDate = first.TryGetProperty("first_air_date", out var fadEl) ? fadEl.GetString() : null;
                    var overview = first.TryGetProperty("overview", out var ovEl) ? ovEl.GetString() : null;
                    var poster = first.TryGetProperty("poster_path", out var ppEl) ? ppEl.GetString() : null;

                    var episodeDetails = await GetEpisodeDetailsAsync(id, season, episode, cancellationToken).ConfigureAwait(false);
                    return new TvShowMetadata(
                        name ?? showName,
                        Helpers.ExtractYear(firstAirDate),
                        season,
                        episode,
                        episodeDetails?.Name,
                        source,
                        episodeDetails?.Overview ?? overview,
                        Helpers.GetPosterUrl(poster));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Fallback parsing failed for TV search response");
        }

        return null;
    }

    private static class Helpers
    {
        public static int ExtractYear(string? dateString)
        {
            return string.IsNullOrWhiteSpace(dateString) || !DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? 0
                : date.Year;
        }

        public static string? GetPosterUrl(string? posterPath)
        {
            return string.IsNullOrWhiteSpace(posterPath) ? null : $"https://image.tmdb.org/t/p/w500{posterPath}";
        }
    }
}
