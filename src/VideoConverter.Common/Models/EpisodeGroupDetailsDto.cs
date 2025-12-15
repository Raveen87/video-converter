namespace VideoConverter.Common.Models;

/// <summary>
/// Detailed episode group with full episode listing.
/// </summary>
public class EpisodeGroupDetailsDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IList<EpisodeGroupSeasonDto> Seasons { get; set; } = [];
}
