namespace VideoConverter.Common.Models;

public class ResolveMetadataRequestDto
{
    public int TmdbId { get; set; }

    public MediaType MediaType { get; set; }

    /// <summary>
    /// Gets or sets the optional episode group ID for TV shows.
    /// When provided, the season/episode numbers will be mapped according to the group's ordering.
    /// </summary>
    public string? EpisodeGroupId { get; set; }
}
