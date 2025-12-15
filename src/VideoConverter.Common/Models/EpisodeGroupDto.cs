namespace VideoConverter.Common.Models;

/// <summary>
/// Episode group summary returned when listing groups for a show.
/// </summary>
public class EpisodeGroupDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of episode group (e.g., "Aired Order", "DVD", "Absolute").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the number of sub-groups (seasons) in this episode group.
    /// </summary>
    public int GroupCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of episodes in this episode group.
    /// </summary>
    public int EpisodeCount { get; set; }
}
