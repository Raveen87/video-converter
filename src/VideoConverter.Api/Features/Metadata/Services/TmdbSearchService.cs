using VideoConverter.Api.Features.Metadata.Dtos;

namespace VideoConverter.Api.Features.Metadata.Services;

public class TmdbSearchService(
    ILogger<TmdbSearchService> logger,
    HttpClient httpClient,
    IConfiguration configuration,
    Func<TimeSpan, CancellationToken, Task>? delayAsync = null) : TmdbServiceBase(logger, httpClient, configuration, delayAsync), ITmdbSearchService
{
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

            Logger.LogDebug("TMDb movie search: q={Query} year={Year}", q, year);

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
            Logger.LogError(ex, "TMDb movie search failed for q={Query}", q);
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

            Logger.LogDebug("TMDb TV search: q={Query} firstAirYear={Year}", q, firstAirYear);

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
            Logger.LogError(ex, "TMDb TV search failed for q={Query}", q);
            return Array.Empty<TvShowSearchItemDto>();
        }
    }
}
