namespace VideoConverter.Api.Features.Metadata.Services;

/// <summary>
/// Service for fetching and working with TMDb episode groups.
/// </summary>
public interface IEpisodeGroupService
{
    /// <summary>
    /// Gets all available episode groups for a TV show.
    /// </summary>
    /// <param name="showId">The TMDb show ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of episode group summaries, or empty if none exist.</returns>
    Task<IEnumerable<EpisodeGroupDto>> GetEpisodeGroupsAsync(int showId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific episode group including all episodes.
    /// </summary>
    /// <param name="groupId">The TMDb episode group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The episode group details, or null if not found.</returns>
    Task<EpisodeGroupDetailsDto?> GetEpisodeGroupDetailsAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps an original season/episode to the group's ordering.
    /// </summary>
    /// <param name="groupDetails">The episode group details.</param>
    /// <param name="originalSeason">The original season number.</param>
    /// <param name="originalEpisode">The original episode number.</param>
    /// <returns>The episode in the group's ordering, or null if not found.</returns>
    EpisodeGroupEpisodeDto? MapEpisodeToGroup(EpisodeGroupDetailsDto groupDetails, int originalSeason, int originalEpisode);

    /// <summary>
    /// Gets an episode by its position within the group's ordering.
    /// Use this when the input file's season/episode numbers represent the group's ordering.
    /// </summary>
    /// <param name="groupDetails">The episode group details.</param>
    /// <param name="groupSeason">The season number within the group's ordering.</param>
    /// <param name="groupEpisode">The episode number within the group's ordering.</param>
    /// <returns>The episode at that position in the group, or null if not found.</returns>
    EpisodeGroupEpisodeDto? GetEpisodeByGroupPosition(EpisodeGroupDetailsDto groupDetails, int groupSeason, int groupEpisode);
}
