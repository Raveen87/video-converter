using System.Globalization;
using System.Text.Json;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public class TmdbMetadataService(HttpClient httpClient, ILogger<TmdbMetadataService> logger, IConfiguration configuration) : IMetadataService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<TmdbMetadataService> _logger = logger;
    private readonly string _apiKey = configuration["Tmdb:ApiKey"] ?? throw new InvalidOperationException("TMDb API key not configured");

    public async Task<MovieMetadata?> GetMovieMetadataAsync(string title, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(title);
            var yearParam = year.HasValue ? $"&year={year.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/movie?api_key={_apiKey}&query={query}{yearParam}";

            _logger.LogDebug("Searching for movie: {Title} (Year: {Year})", title, year);

            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var searchResult = JsonSerializer.Deserialize<TmdbMovieSearchResult>(response);

            if (searchResult?.Results?.Count > 0)
            {
                var movie = searchResult.Results[0];
                return new MovieMetadata(
                    movie.Title,
                    ExtractYear(movie.ReleaseDate),
                    movie.Overview,
                    GetPosterUrl(movie.PosterPath)
                );
            }

            _logger.LogWarning("No movie found for: {Title} (Year: {Year})", title, year);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movie metadata for: {Title}", title);
            return null;
        }
    }

    public async Task<TvShowMetadata?> GetTvShowMetadataAsync(string showName, int season, int episode, int? firstAiredYear = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(showName);
            var yearParam = firstAiredYear.HasValue ? $"&first_air_date_year={firstAiredYear.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/tv?api_key={_apiKey}&query={query}{yearParam}";

            _logger.LogDebug("Searching for TV show: {ShowName} (Year: {Year})", showName, firstAiredYear);

            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var searchResult = JsonSerializer.Deserialize<TmdbTvSearchResult>(response);

            if (searchResult?.Results?.Count > 0)
            {
                var show = searchResult.Results[0];

                // Get episode details
                var episodeDetails = await GetEpisodeDetailsAsync(show.Id, season, episode, cancellationToken);

                return new TvShowMetadata(
                    show.Name,
                    ExtractYear(show.FirstAirDate),
                    season,
                    episode,
                    episodeDetails?.Name,
                    episodeDetails?.Overview ?? show.Overview,
                    GetPosterUrl(show.PosterPath)
                );
            }

            _logger.LogWarning("No TV show found for: {ShowName} (Year: {Year})", showName, firstAiredYear);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching TV show metadata for: {ShowName}", showName);
            return null;
        }
    }

    private async Task<TmdbEpisodeDetails?> GetEpisodeDetailsAsync(int showId, int season, int episode, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{showId}/season/{season}/episode/{episode}?api_key={_apiKey}";
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            return JsonSerializer.Deserialize<TmdbEpisodeDetails>(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching episode details for show {ShowId}, season {Season}, episode {Episode}", showId, season, episode);
            return null;
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

    // TMDb API response models
    private sealed record TmdbMovieSearchResult(List<TmdbMovie>? Results);
    private sealed record TmdbMovie(string Title, string? ReleaseDate, string? Overview, string? PosterPath);

    private sealed record TmdbTvSearchResult(List<TmdbTvShow>? Results);
    private sealed record TmdbTvShow(int Id, string Name, string? FirstAirDate, string? Overview, string? PosterPath);

    private sealed record TmdbEpisodeDetails(string Name, string? Overview);
}
