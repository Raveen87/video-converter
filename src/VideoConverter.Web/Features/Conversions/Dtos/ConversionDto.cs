using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Conversions.Dtos;

public record ConversionDto(
    int Id,
    string InputPath,
    string OutputDirectory,
    DateTime CreatedAtUtc,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename);
