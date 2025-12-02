namespace VideoConverter.Common.Models;

public class ResolveBatchMetadataRequestDto
{
    public IEnumerable<int> ConversionIds { get; set; } = [];

    public int TmdbId { get; set; }

    public MediaType MediaType { get; set; }
}
