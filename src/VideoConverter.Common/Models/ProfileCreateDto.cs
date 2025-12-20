namespace VideoConverter.Common.Models;

public record ProfileCreateDto(
    string Name,
    string? OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    IReadOnlyList<string>? AudioLanguages,
    IReadOnlyList<string>? SubtitleLanguages,
    string? OutputFilenameTemplate = null,
    string? OutputPathTemplate = null,
    bool VmafEnabled = false);
