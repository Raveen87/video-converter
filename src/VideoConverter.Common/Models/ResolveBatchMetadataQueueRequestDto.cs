namespace VideoConverter.Common.Models;

public class ResolveBatchMetadataQueueRequestDto
{
    public IEnumerable<int> QueueItemIds { get; set; } = [];

    public int TmdbId { get; set; }

    public MediaType MediaType { get; set; }

    /// <summary>
    /// Gets or sets the optional episode group ID for TV shows.
    /// When provided, the season/episode numbers will be mapped according to the group's ordering for all items.
    /// </summary>
    public string? EpisodeGroupId { get; set; }
}
