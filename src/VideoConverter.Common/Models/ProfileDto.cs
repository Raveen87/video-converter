namespace VideoConverter.Common.Models;

public record ProfileDto(
    int Id,
    string Name,
    string? OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    string? OutputFilenameTemplate,
    IEnumerable<string>? AudioLanguages,
    IEnumerable<string>? SubtitleLanguages);
