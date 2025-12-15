namespace VideoConverter.Common.Models;

/// <summary>
/// An episode within an episode group season.
/// </summary>
public class EpisodeGroupEpisodeDto
{
    public int TmdbEpisodeId { get; set; }

    /// <summary>
    /// Gets or sets the episode number within this group's season.
    /// </summary>
    public int Order { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Overview { get; set; }

    /// <summary>
    /// Gets or sets the original season number (for reference).
    /// </summary>
    public int? OriginalSeason { get; set; }

    /// <summary>
    /// Gets or sets the original episode number (for reference).
    /// </summary>
    public int? OriginalEpisode { get; set; }
}
