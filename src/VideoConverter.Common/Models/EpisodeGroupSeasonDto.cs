namespace VideoConverter.Common.Models;

/// <summary>
/// A season within an episode group.
/// </summary>
public class EpisodeGroupSeasonDto
{
    /// <summary>
    /// Gets or sets the season number within this group.
    /// </summary>
    public int Order { get; set; }

    public string? Name { get; set; }

    public IList<EpisodeGroupEpisodeDto> Episodes { get; set; } = [];
}
