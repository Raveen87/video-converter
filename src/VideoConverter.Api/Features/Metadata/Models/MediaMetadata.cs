namespace VideoConverter.Api.Features.Metadata.Models;

public abstract record MediaMetadata(MediaType Type, int? TmdbId = null);
