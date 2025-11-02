using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public class TmdbMetadataService(
    ILogger<TmdbMetadataService> logger,
    HttpClient httpClient,
    IConfiguration configuration) : IMetadataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _apiKey = configuration["Tmdb:ApiKey"] ?? throw new InvalidOperationException("TMDb API key not configured");

    public async Task<MovieMetadata?> GetMovieMetadataAsync(string title, int? year = null, string? source = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(title);
            var yearParam = year is not null ? $"&year={year.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/movie?query={query}{yearParam}";

            logger.LogDebug("Searching for movie: {Title} (Year: {Year})", title, year);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var searchResult = JsonSerializer.Deserialize<TmdbMovieSearchResult>(responseString, JsonOptions);

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
            var searchResult = JsonSerializer.Deserialize<TmdbTvSearchResult>(responseString, JsonOptions);

            if (searchResult?.Results?.Count > 0)
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

            logger.LogWarning("No TV show found for: {ShowName} (Year: {Year})", showName, firstAiredYear);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching TV show metadata for: {ShowName}", showName);
            return null;
        }
    }

    private async Task<string> GetAuthorizedStringAsync(string url, CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        using var response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<TmdbEpisodeDetails?> GetEpisodeDetailsAsync(int showId, int season, int episode, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{showId}/season/{season}/episode/{episode}";
            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TmdbEpisodeDetails>(responseString, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error fetching episode details for show {ShowId}, season {Season}, episode {Episode}", showId, season, episode);
            return null;
        }
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

    // TMDb API response models
    private sealed record TmdbMovieSearchResult([property: JsonPropertyName("results")] List<TmdbMovie>? Results);

    private sealed record TmdbMovie(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("poster_path")] string? PosterPath);

    private sealed record TmdbTvSearchResult([property: JsonPropertyName("results")] List<TmdbTvShow>? Results);

    private sealed record TmdbTvShow(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("poster_path")] string? PosterPath);

    private sealed record TmdbEpisodeDetails(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("overview")] string? Overview);
}
