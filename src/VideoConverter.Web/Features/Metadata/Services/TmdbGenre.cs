using System.Text.Json.Serialization;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed record TmdbGenre(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name
);
