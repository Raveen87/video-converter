using System.Text.Json.Serialization;

namespace VideoConverter.Web.Models;

public record ConversionDto(
    int Id,
    string InputPath,
    string OutputDirectory,
    DateTime CreatedAtUtc,
    int? Crf,
    int? AudioBitrateKbps,
    [property: JsonConverter(typeof(AudioChannelsConverter))]
    string? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename);
