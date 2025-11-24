using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Conversions.Dtos;

public record ConversionDto(
    int Id,
    string InputPath,
    string OutputDirectory,
    DateTime CreatedAtUtc,
    ConversionStatus Status,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename);
