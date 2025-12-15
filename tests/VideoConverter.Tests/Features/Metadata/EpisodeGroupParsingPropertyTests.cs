using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Common.Models;
using static VideoConverter.Api.Features.Metadata.Services.TmdbJson;

namespace VideoConverter.Tests.Features.Metadata;

/// <summary>
/// Property-based tests for episode group response parsing.
/// Feature: tmdb-episode-groups, Property 1: Episode group list contains required fields.
/// Validates: Requirements 1.2.
/// </summary>
public class EpisodeGroupParsingPropertyTests
{
    /// <summary>
    /// Feature: tmdb-episode-groups, Property 1: Episode group list contains required fields.
    /// Validates: Requirements 1.2.
    /// For any valid TMDb episode groups response, the mapped EpisodeGroupDto list SHALL contain
    /// all groups with their ID, name, type, description, and episode count correctly populated.
    /// </summary>
    /// <param name="groupCounts">The generated group counts used to create test data.</param>
    /// <returns>A property that validates the mapping.</returns>
    [Property(MaxTest = 100)]
    public Property EpisodeGroupListContainsRequiredFields(NonEmptyArray<PositiveInt> groupCounts)
    {
        // Generate test data from the random inputs
        var summaries = GenerateSummaries(groupCounts.Item);

        // Build JSON from the generated summaries
        var json = BuildEpisodeGroupsJson(summaries);

        // Parse the JSON
        var response = TmdbJson.DeserializeEpisodeGroupsResponse(json);

        // Map to DTOs
        var dtos = MapToEpisodeGroupDtos(response);

        // Property: All fields should be correctly mapped
        var countMatches = dtos.Count == summaries.Count;
        var fieldsMatch = AllFieldsCorrectlyMapped(summaries, dtos);

        return (countMatches && fieldsMatch).Label("All fields correctly mapped");
    }

    private static List<TmdbEpisodeGroupSummaryData> GenerateSummaries(PositiveInt[] groupCounts)
    {
        var ids = new[] { "5acf93e60e0a26346d0000ce", "5b12e3f8c3a368736c0041d7", "abc123", "def456", "ghi789" };
        var names = new[] { "Aired Order", "DVD Order", "Absolute Order", "Netflix Order", "Production Order" };
        var descriptions = new string?[] { null, "Original broadcast order", "DVD release order", "Streaming order" };

        return groupCounts.Select((gc, i) => new TmdbEpisodeGroupSummaryData(
            Id: ids[i % ids.Length] + i,
            Name: names[i % names.Length],
            Type: (i % 7) + 1,
            Description: descriptions[i % descriptions.Length],
            GroupCount: (gc.Item % 10) + 1,
            EpisodeCount: (gc.Item % 200) + 1)).ToList();
    }

    private static string BuildEpisodeGroupsJson(List<TmdbEpisodeGroupSummaryData> summaries)
    {
        var results = summaries.Select(s =>
            $$"""{"id":"{{s.Id}}","name":"{{EscapeJson(s.Name)}}","type":{{s.Type}},"description":{{(s.Description == null ? "null" : $"\"{EscapeJson(s.Description)}\"")}},"group_count":{{s.GroupCount}},"episode_count":{{s.EpisodeCount}}}""");

        return $$"""{"results":[{{string.Join(",", results)}}]}""";
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static List<EpisodeGroupDto> MapToEpisodeGroupDtos(TmdbEpisodeGroupsResponse? response)
    {
        if (response?.Results == null)
        {
            return [];
        }

        return response.Results.Select(r => new EpisodeGroupDto
        {
            Id = r.Id,
            Name = r.Name,
            Type = EpisodeGroupTypeMapper.GetTypeName(r.Type),
            Description = r.Description,
            GroupCount = r.GroupCount,
            EpisodeCount = r.EpisodeCount,
        }).ToList();
    }

    private static bool AllFieldsCorrectlyMapped(
        List<TmdbEpisodeGroupSummaryData> summaries,
        List<EpisodeGroupDto> dtos)
    {
        if (summaries.Count != dtos.Count)
        {
            return false;
        }

        for (var i = 0; i < summaries.Count; i++)
        {
            var summary = summaries[i];
            var dto = dtos[i];

            if (!string.Equals(dto.Id, summary.Id, StringComparison.Ordinal) ||
                !string.Equals(dto.Name, summary.Name, StringComparison.Ordinal) ||
                !string.Equals(dto.Type, EpisodeGroupTypeMapper.GetTypeName(summary.Type), StringComparison.Ordinal) ||
                !string.Equals(dto.Description, summary.Description, StringComparison.Ordinal) ||
                dto.GroupCount != summary.GroupCount ||
                dto.EpisodeCount != summary.EpisodeCount)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Test data record for episode group summary generation.
    /// </summary>
    public record TmdbEpisodeGroupSummaryData(
        string Id,
        string Name,
        int Type,
        string? Description,
        int GroupCount,
        int EpisodeCount);
}
