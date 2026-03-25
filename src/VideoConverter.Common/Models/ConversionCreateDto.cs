namespace VideoConverter.Common.Models;

public record ConversionCreateDto(
    string InputPath,
    string OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    IEnumerable<string>? AudioLanguages,
    IEnumerable<string>? SubtitleLanguages,
    string? OutputFilenameTemplate = null,
    string? OutputPathTemplate = null,
    bool? VmafEnabled = null,
    double? RequiredVmaf = null);
