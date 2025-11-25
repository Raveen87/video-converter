using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Profiles.Dtos;

public record ProfileCreateDto(
    string Name,
    string? OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename,
    IReadOnlyList<string>? AudioLanguages,
    IReadOnlyList<string>? SubtitleLanguages,
    string? OutputFilenameTemplate = null);
