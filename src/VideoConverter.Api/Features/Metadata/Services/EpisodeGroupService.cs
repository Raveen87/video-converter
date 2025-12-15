namespace VideoConverter.Api.Features.Metadata.Services;

/// <summary>
/// Service for fetching and working with TMDb episode groups.
/// </summary>
public class EpisodeGroupService(
    ILogger<EpisodeGroupService> logger,
    HttpClient httpClient,
    IConfiguration configuration,
    Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    : TmdbServiceBase(logger, httpClient, configuration, delayAsync), IEpisodeGroupService
{
    /// <inheritdoc />
    public async Task<IEnumerable<EpisodeGroupDto>> GetEpisodeGroupsAsync(int showId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{showId}?append_to_response=episode_groups";
            Logger.LogDebug("Fetching episode groups for show {ShowId}", showId);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);

            // The episode_groups field is nested in the response
            using var doc = JsonDocument.Parse(responseString);
            if (!doc.RootElement.TryGetProperty("episode_groups", out var episodeGroupsElement))
            {
                Logger.LogDebug("No episode_groups field found for show {ShowId}", showId);
                return [];
            }

            var episodeGroupsJson = episodeGroupsElement.GetRawText();
            var response = TmdbJson.DeserializeEpisodeGroupsResponse(episodeGroupsJson);

            if (response?.Results is null or { Count: 0 })
            {
                Logger.LogDebug("No episode groups found for show {ShowId}", showId);
                return [];
            }

            return response.Results.Select(MapToDto);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            Logger.LogWarning(ex, "TV show {ShowId} not found", showId);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<EpisodeGroupDetailsDto?> GetEpisodeGroupDetailsAsync(string groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/episode_group/{groupId}";
            Logger.LogDebug("Fetching episode group details for group {GroupId}", groupId);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var response = TmdbJson.DeserializeEpisodeGroupDetailResponse(responseString);

            if (response is null)
            {
                Logger.LogWarning("Failed to deserialize episode group details for {GroupId}", groupId);
                return null;
            }

            return MapToDetailsDto(response);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            Logger.LogWarning(ex, "Episode group {GroupId} not found", groupId);
            return null;
        }
    }

    /// <inheritdoc />
    public EpisodeGroupEpisodeDto? MapEpisodeToGroup(EpisodeGroupDetailsDto groupDetails, int originalSeason, int originalEpisode)
    {
        foreach (var season in groupDetails.Seasons)
        {
            foreach (var episode in season.Episodes)
            {
                if (episode.OriginalSeason == originalSeason && episode.OriginalEpisode == originalEpisode)
                {
                    return episode;
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public EpisodeGroupEpisodeDto? GetEpisodeByGroupPosition(EpisodeGroupDetailsDto groupDetails, int groupSeason, int groupEpisode)
    {
        var season = groupDetails.Seasons.FirstOrDefault(s => s.Order == groupSeason);
        return season?.Episodes.FirstOrDefault(e => e.Order == groupEpisode);
    }

    private static EpisodeGroupDto MapToDto(TmdbJson.TmdbEpisodeGroupSummary summary)
    {
        return new EpisodeGroupDto
        {
            Id = summary.Id,
            Name = summary.Name,
            Type = EpisodeGroupTypeMapper.GetTypeName(summary.Type),
            Description = summary.Description,
            GroupCount = summary.GroupCount,
            EpisodeCount = summary.EpisodeCount,
        };
    }

    private static EpisodeGroupDetailsDto MapToDetailsDto(TmdbJson.TmdbEpisodeGroupDetailResponse response)
    {
        var seasons = response.Groups?
            .OrderBy(g => g.Order)
            .Select(MapToSeasonDto)
            .ToList() ?? [];

        return new EpisodeGroupDetailsDto
        {
            Id = response.Id,
            Name = response.Name,
            Type = EpisodeGroupTypeMapper.GetTypeName(response.Type),
            Description = response.Description,
            Seasons = seasons,
        };
    }

    private static EpisodeGroupSeasonDto MapToSeasonDto(TmdbJson.TmdbEpisodeGroupPart part)
    {
        var episodes = part.Episodes?
            .OrderBy(e => e.Order)
            .Select(MapToEpisodeDto)
            .ToList() ?? [];

        return new EpisodeGroupSeasonDto
        {
            Order = part.Order,
            Name = part.Name,
            Episodes = episodes,
        };
    }

    private static EpisodeGroupEpisodeDto MapToEpisodeDto(TmdbJson.TmdbEpisodeGroupEpisode episode)
    {
        return new EpisodeGroupEpisodeDto
        {
            TmdbEpisodeId = episode.Id,
            Order = episode.Order,
            Name = episode.Name,
            Overview = episode.Overview,
            OriginalSeason = episode.SeasonNumber,
            OriginalEpisode = episode.EpisodeNumber,
        };
    }
}
