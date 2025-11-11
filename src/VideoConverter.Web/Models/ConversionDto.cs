namespace VideoConverter.Web.Models;

public record ConversionDto(
    int Id,
    string InputPath,
    string OutputDirectory,
    DateTime CreatedAtUtc,
    int? Crf,
    int? AudioBitrateKbps,
    string? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename);
