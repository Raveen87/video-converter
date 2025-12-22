namespace VideoConverter.Common.Models;

public record ProfileDirectoryQueueRequestDto(
    string DirectoryPath,
    string OutputDirectory,
    IEnumerable<string>? Extensions,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    IEnumerable<string>? AudioLanguages,
    IEnumerable<string>? SubtitleLanguages,
    string? OutputFilenameTemplate,
    string? OutputPathTemplate,
    bool? VmafEnabled = null);
