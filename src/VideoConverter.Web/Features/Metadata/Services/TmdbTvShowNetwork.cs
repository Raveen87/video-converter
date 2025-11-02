using System.Text.Json.Serialization;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed record TmdbTvShowNetwork(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("origin_country")] string? OriginCountry,
    [property: JsonPropertyName("logo_path")] string? LogoPath
);
