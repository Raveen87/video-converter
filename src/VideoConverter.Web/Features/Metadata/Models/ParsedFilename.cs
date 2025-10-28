namespace VideoConverter.Web.Features.Metadata.Models;

public record ParsedFilename(
    string OriginalFilename,
    MediaType DetectedType,
    string? Title = null,
    int? Year = null,
    int? Season = null,
    int? Episode = null,
    string? EpisodeTitle = null,
    string? Source = null
);
