namespace VideoConverter.Common.Models;

public record DirectoryQueueRequestDto(
    string DirectoryPath,
    string OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    IEnumerable<string>? AudioLanguages,
    IEnumerable<string>? SubtitleLanguages,
    string? OutputFilenameTemplate,
    IEnumerable<string>? Extensions);
