namespace VideoConverter.Common.Models;

public class MetadataSearchResultDto
{
    public int Id { get; set; } // TmdbId

    public string Title { get; set; } = string.Empty;

    public int? Year { get; set; }

    public string? Overview { get; set; }

    public string? PosterUrl { get; set; }

    public MediaType Type { get; set; }
}
