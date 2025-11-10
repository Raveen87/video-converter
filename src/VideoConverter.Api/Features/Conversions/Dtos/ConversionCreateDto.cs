using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Conversions.Dtos;

public record ConversionCreateDto(
    string InputPath,
    string OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    IEnumerable<string>? AudioLanguages,
    IEnumerable<string>? SubtitleLanguages,
    string? OutputFilenameTemplate = null);
