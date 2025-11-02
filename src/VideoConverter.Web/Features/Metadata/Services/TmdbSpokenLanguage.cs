using System.Text.Json.Serialization;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed record TmdbSpokenLanguage(
    [property: JsonPropertyName("english_name")] string EnglishName,
    [property: JsonPropertyName("iso_639_1")] string Iso6391,
    [property: JsonPropertyName("name")] string Name
);
