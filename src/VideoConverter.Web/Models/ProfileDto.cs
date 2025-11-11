using System.Text.Json.Serialization;

namespace VideoConverter.Web.Models;

public record ProfileDto(
    int Id,
    string? OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    [property: JsonConverter(typeof(AudioChannelsConverter))]
    string? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename,
    string? OutputFilenameTemplate,
    IEnumerable<string>? AudioLanguages,
    IEnumerable<string>? SubtitleLanguages);
