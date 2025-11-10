using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Profiles.Dtos;

public record ProfileDto(
    int Id,
    string? OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename,
    string? OutputFilenameTemplate,
    IEnumerable<string>? AudioLanguages,
    IEnumerable<string>? SubtitleLanguages);
