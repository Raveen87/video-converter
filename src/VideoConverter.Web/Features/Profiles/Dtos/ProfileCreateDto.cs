using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Profiles.Dtos;

public record ProfileCreateDto(
    string? OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename,
    IReadOnlyList<string>? AudioLanguages,
    IReadOnlyList<string>? SubtitleLanguages,
    string? OutputFilenameTemplate = null);
