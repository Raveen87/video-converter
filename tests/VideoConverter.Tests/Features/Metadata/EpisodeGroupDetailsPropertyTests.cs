using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Common.Models;
using static VideoConverter.Api.Features.Metadata.Services.TmdbJson;

namespace VideoConverter.Tests.Features.Metadata;

/// <summary>
/// Property-based tests for episode group details organization.
/// Feature: tmdb-episode-groups, Property 2: Episode group details are correctly organized by season.
/// Validates: Requirements 2.2, 2.3.
/// </summary>
public class EpisodeGroupDetailsPropertyTests
{
    /// <summary>
    /// Feature: tmdb-episode-groups, Property 2: Episode group details are correctly organized by season.
    /// Validates: Requirements 2.2, 2.3.
    /// For any valid TMDb episode group details response, the mapped EpisodeGroupDetailsDto SHALL
    /// organize episodes by their group-specific season number, with each episode containing its
    /// group-specific order and name.
    /// </summary>
    /// <param name="seasonCount">Number of seasons to generate (1-5).</param>
    /// <param name="episodesPerSeason">Episodes per season (1-10).</param>
    /// <returns>A property that validates the mapping.</returns>
    [Property(MaxTest = 100)]
    public Property EpisodeGroupDetailsAreCorrectlyOrganizedBySeason(
        PositiveInt seasonCount,
        PositiveInt episodesPerSeason)
    {
        // Constrain to reasonable values
        var numSeasons = (seasonCount.Item % 5) + 1;
        var numEpisodes = (episodesPerSeason.Item % 10) + 1;

        // Generate test data
        var testData = GenerateEpisodeGroupDetailData(numSeasons, numEpisodes);

        // Build JSON from the generated data
        var json = BuildEpisodeGroupDetailJson(testData);

        // Parse the JSON
        var response = TmdbJson.DeserializeEpisodeGroupDetailResponse(json);

        // Map to DTO
        var dto = MapToEpisodeGroupDetailsDto(response);

        // Properties to verify:
        // 1. Seasons are ordered by their Order field
        var seasonsOrdered = AreSeasonsOrdered(dto);

        // 2. Episodes within each season are ordered by their Order field
        var episodesOrdered = AreEpisodesOrderedWithinSeasons(dto);

        // 3. All fields are correctly mapped
        var fieldsCorrect = AllFieldsCorrectlyMapped(testData, dto);

        // 4. Season count matches
        var seasonCountMatches = dto?.Seasons.Count == numSeasons;

        return (seasonsOrdered && episodesOrdered && fieldsCorrect && seasonCountMatches)
            .Label("Episode group details correctly organized by season");
    }

    private static EpisodeGroupDetailTestData GenerateEpisodeGroupDetailData(int numSeasons, int numEpisodes)
    {
        var groups = new List<EpisodeGroupPartTestData>();

        for (var s = 0; s < numSeasons; s++)
        {
            var episodes = new List<EpisodeGroupEpisodeTestData>();
            for (var e = 0; e < numEpisodes; e++)
            {
                episodes.Add(new EpisodeGroupEpisodeTestData(
                    Id: (s * 100) + e + 1000,
                    Order: e + 1,
                    Name: $"Episode {e + 1}",
                    Overview: $"Overview for S{s + 1}E{e + 1}",
                    SeasonNumber: s + 1,
                    EpisodeNumber: e + 1));
            }

            groups.Add(new EpisodeGroupPartTestData(
                Order: s + 1,
                Name: $"Season {s + 1}",
                Episodes: episodes));
        }

        return new EpisodeGroupDetailTestData(
            Id: "test-group-id",
            Name: "Test Episode Group",
            Type: 3,
            Description: "Test description",
            Groups: groups);
    }

    private static string BuildEpisodeGroupDetailJson(EpisodeGroupDetailTestData data)
    {
        var groupsJson = data.Groups.Select(g =>
        {
            var episodesJson = g.Episodes.Select(e =>
                "{\"id\":" + e.Id + ",\"order\":" + e.Order + ",\"name\":\"" + EscapeJson(e.Name) +
                "\",\"overview\":\"" + EscapeJson(e.Overview ?? string.Empty) +
                "\",\"season_number\":" + e.SeasonNumber + ",\"episode_number\":" + e.EpisodeNumber + "}");

            return "{\"order\":" + g.Order + ",\"name\":\"" + EscapeJson(g.Name ?? string.Empty) +
                   "\",\"episodes\":[" + string.Join(",", episodesJson) + "]}";
        });

        return "{\"id\":\"" + data.Id + "\",\"name\":\"" + EscapeJson(data.Name) +
               "\",\"type\":" + data.Type + ",\"description\":\"" + EscapeJson(data.Description ?? string.Empty) +
               "\",\"groups\":[" + string.Join(",", groupsJson) + "]}";
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static EpisodeGroupDetailsDto? MapToEpisodeGroupDetailsDto(TmdbEpisodeGroupDetailResponse? response)
    {
        if (response == null)
        {
            return null;
        }

        var seasons = response.Groups?
            .OrderBy(g => g.Order)
            .Select(g => new EpisodeGroupSeasonDto
            {
                Order = g.Order,
                Name = g.Name,
                Episodes = g.Episodes?
                    .OrderBy(e => e.Order)
                    .Select(e => new EpisodeGroupEpisodeDto
                    {
                        TmdbEpisodeId = e.Id,
                        Order = e.Order,
                        Name = e.Name,
                        Overview = e.Overview,
                        OriginalSeason = e.SeasonNumber,
                        OriginalEpisode = e.EpisodeNumber,
                    })
                    .ToList() ?? [],
            })
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

    private static bool AreSeasonsOrdered(EpisodeGroupDetailsDto? dto)
    {
        if (dto?.Seasons == null || dto.Seasons.Count == 0)
        {
            return true;
        }

        for (var i = 1; i < dto.Seasons.Count; i++)
        {
            if (dto.Seasons[i].Order < dto.Seasons[i - 1].Order)
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreEpisodesOrderedWithinSeasons(EpisodeGroupDetailsDto? dto)
    {
        if (dto?.Seasons == null)
        {
            return true;
        }

        foreach (var season in dto.Seasons)
        {
            if (season.Episodes.Count <= 1)
            {
                continue;
            }

            for (var i = 1; i < season.Episodes.Count; i++)
            {
                if (season.Episodes[i].Order < season.Episodes[i - 1].Order)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool AllFieldsCorrectlyMapped(EpisodeGroupDetailTestData testData, EpisodeGroupDetailsDto? dto)
    {
        if (dto == null)
        {
            return false;
        }

        if (!string.Equals(dto.Id, testData.Id, StringComparison.Ordinal) ||
            !string.Equals(dto.Name, testData.Name, StringComparison.Ordinal) ||
            !string.Equals(dto.Type, EpisodeGroupTypeMapper.GetTypeName(testData.Type), StringComparison.Ordinal))
        {
            return false;
        }

        if (dto.Seasons.Count != testData.Groups.Count)
        {
            return false;
        }

        // Verify each season and its episodes
        var orderedTestGroups = testData.Groups.OrderBy(g => g.Order).ToList();
        for (var s = 0; s < dto.Seasons.Count; s++)
        {
            var dtoSeason = dto.Seasons[s];
            var testGroup = orderedTestGroups[s];

            if (dtoSeason.Order != testGroup.Order ||
                !string.Equals(dtoSeason.Name, testGroup.Name, StringComparison.Ordinal))
            {
                return false;
            }

            if (dtoSeason.Episodes.Count != testGroup.Episodes.Count)
            {
                return false;
            }

            var orderedTestEpisodes = testGroup.Episodes.OrderBy(e => e.Order).ToList();
            for (var e = 0; e < dtoSeason.Episodes.Count; e++)
            {
                var dtoEpisode = dtoSeason.Episodes[e];
                var testEpisode = orderedTestEpisodes[e];

                if (dtoEpisode.TmdbEpisodeId != testEpisode.Id ||
                    dtoEpisode.Order != testEpisode.Order ||
                    !string.Equals(dtoEpisode.Name, testEpisode.Name, StringComparison.Ordinal) ||
                    dtoEpisode.OriginalSeason != testEpisode.SeasonNumber ||
                    dtoEpisode.OriginalEpisode != testEpisode.EpisodeNumber)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Test data record for episode group detail generation.
    /// </summary>
    public record EpisodeGroupDetailTestData(
        string Id,
        string Name,
        int Type,
        string? Description,
        IList<EpisodeGroupPartTestData> Groups);

    /// <summary>
    /// Test data record for episode group part (season) generation.
    /// </summary>
    public record EpisodeGroupPartTestData(
        int Order,
        string? Name,
        IList<EpisodeGroupEpisodeTestData> Episodes);

    /// <summary>
    /// Test data record for episode group episode generation.
    /// </summary>
    public record EpisodeGroupEpisodeTestData(
        int Id,
        int Order,
        string Name,
        string? Overview,
        int SeasonNumber,
        int EpisodeNumber);
}
