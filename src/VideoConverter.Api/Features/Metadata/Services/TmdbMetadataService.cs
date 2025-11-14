using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public class TmdbMetadataService(
    ILogger<TmdbMetadataService> logger,
    HttpClient httpClient,
    IConfiguration configuration,
    Func<TimeSpan, CancellationToken, Task>? delayAsync = null) : TmdbServiceBase(logger, httpClient, configuration, delayAsync), IMetadataService
{
    public async Task<MovieMetadata?> GetMovieMetadataAsync(string title, int? year = null, string? source = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(title);
            var yearParam = year is not null ? $"&year={year.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/movie?query={query}{yearParam}";

            Logger.LogDebug("Searching for movie: {Title} (Year: {Year})", title, year);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var searchResult = TmdbJson.DeserializeMovieSearchResult(responseString);

            if (searchResult?.Results?.Count > 0)
            {
                var movie = searchResult.Results[0];
                return new MovieMetadata(
                    movie.Title,
                    ExtractYear(movie.ReleaseDate),
                    source,
                    movie.Overview,
                    GetPosterUrl(movie.PosterPath));
            }

            Logger.LogWarning("No movie found for: {Title} (Year: {Year})", title, year);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching movie metadata for: {Title}", title);
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

            Logger.LogDebug("Searching for TV show: {ShowName} (Year: {Year})", showName, firstAiredYear);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var searchResult = TmdbJson.DeserializeTvSearchResult(responseString);

            if (searchResult?.Results?.Count is > 0)
            {
                var show = searchResult.Results[0];

                // Get episode details
                var episodeDetails = await GetEpisodeDetailsAsync(show.Id, season, episode, cancellationToken).ConfigureAwait(false);

                return new TvShowMetadata(
                    show.Name,
                    ExtractYear(show.FirstAirDate),
                    season,
                    episode,
                    episodeDetails?.Name,
                    source,
                    episodeDetails?.Overview ?? show.Overview,
                    GetPosterUrl(show.PosterPath));
            }

            var fallback = await TryBuildTvShowMetadataFromSearchJsonAsync(responseString, showName, season, episode, source, cancellationToken).ConfigureAwait(false);
            if (fallback is not null)
            {
                return fallback;
            }

            Logger.LogWarning("No TV show found for: {ShowName} (Year: {Year})", showName, firstAiredYear);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching TV show metadata for: {ShowName}", showName);
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
            Logger.LogWarning(ex, "Error fetching TV show details for show {ShowId}", showId);
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
            Logger.LogWarning(ex, "Error fetching season details for show {ShowId}, season {Season}", showId, seasonNumber);
            return null;
        }
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
            Logger.LogWarning(ex, "Error fetching episode details for show {ShowId}, season {Season}, episode {Episode}", showId, season, episode);
            return null;
        }
    }

    private async Task<TvShowMetadata?> TryBuildTvShowMetadataFromSearchJsonAsync(string responseString, string showName, int season, int episode, string? source, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("results", out var resultsEl) && resultsEl.ValueKind is JsonValueKind.Array)
            {
                var first = resultsEl.EnumerateArray().FirstOrDefault();
                if (first.ValueKind is JsonValueKind.Object && first.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var id))
                {
                    var name = first.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : showName;
                    var firstAirDate = first.TryGetProperty("first_air_date", out var fadEl) ? fadEl.GetString() : null;
                    var overview = first.TryGetProperty("overview", out var ovEl) ? ovEl.GetString() : null;
                    var poster = first.TryGetProperty("poster_path", out var ppEl) ? ppEl.GetString() : null;

                    var episodeDetails = await GetEpisodeDetailsAsync(id, season, episode, cancellationToken).ConfigureAwait(false);
                    return new TvShowMetadata(
                        name ?? showName,
                        ExtractYear(firstAirDate),
                        season,
                        episode,
                        episodeDetails?.Name,
                        source,
                        episodeDetails?.Overview ?? overview,
                        GetPosterUrl(poster));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Fallback parsing failed for TV search response");
        }

        return null;
    }
}
