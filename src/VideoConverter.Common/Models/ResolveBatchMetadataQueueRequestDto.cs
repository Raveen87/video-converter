namespace VideoConverter.Common.Models;

public class ResolveBatchMetadataQueueRequestDto
{
    public IEnumerable<int> QueueItemIds { get; set; } = [];

    public int TmdbId { get; set; }

    public MediaType MediaType { get; set; }
}
