namespace VideoConverter.Web.Dtos;

public record ConversionCreateDto(
    string InputPath,
    string OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    int? Resolution,
    IReadOnlyList<string>? AudioLanguages,
    IReadOnlyList<string>? SubtitleLanguages);

public record ConversionDto(
    int Id,
    string InputPath,
    string OutputDirectory,
    DateTime CreatedAtUtc,
    int? Crf,
    int? AudioBitrateKbps,
    int? Resolution,
    string? MetadataBasedFilename);
