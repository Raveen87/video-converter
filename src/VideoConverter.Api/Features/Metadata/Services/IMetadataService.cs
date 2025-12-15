using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public interface IMetadataService
{
    Task<MovieMetadata?> GetMovieMetadataAsync(string title, int? year = null, string? source = null, CancellationToken cancellationToken = default);

    Task<TvShowMetadata?> GetTvShowMetadataAsync(string showName, int season, int episode, int? firstAiredYear = null, string? source = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<MetadataSearchResultDto>> SearchMovieAsync(string query, int? year = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<MetadataSearchResultDto>> SearchTvShowAsync(string query, int? firstAiredYear = null, CancellationToken cancellationToken = default);

    Task<MovieMetadata?> GetMovieMetadataByIdAsync(int tmdbId, string? source = null, CancellationToken cancellationToken = default);

    Task<TvShowMetadata?> GetTvShowMetadataByIdAsync(int tmdbId, int season, int episode, string? source = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets TV show metadata using episode group ordering.
    /// </summary>
    /// <param name="tmdbId">The TMDb show ID.</param>
    /// <param name="groupSeason">The season number according to the episode group.</param>
    /// <param name="groupEpisode">The episode number according to the episode group.</param>
    /// <param name="episodeName">The episode name from the episode group.</param>
    /// <param name="source">Optional source information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The TV show metadata with group-specific ordering, or null if not found.</returns>
    Task<TvShowMetadata?> GetTvShowMetadataByIdWithGroupAsync(
        int tmdbId,
        int groupSeason,
        int groupEpisode,
        string episodeName,
        string? source = null,
        CancellationToken cancellationToken = default);
}
