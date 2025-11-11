namespace VideoConverter.Web.Models;

public record ProfileCreateDto(
    string? OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    string? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename,
    IReadOnlyList<string>? AudioLanguages,
    IReadOnlyList<string>? SubtitleLanguages,
    string? OutputFilenameTemplate = null);
