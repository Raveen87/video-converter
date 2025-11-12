namespace VideoConverter.Web.Models;

public record DirectoryQueueRequestDto(
    string DirectoryPath,
    string OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    string? AudioChannels,
    int? Resolution,
    IEnumerable<string>? AudioLanguages,
    IEnumerable<string>? SubtitleLanguages,
    string? OutputFilenameTemplate,
    IEnumerable<string>? Extensions);
