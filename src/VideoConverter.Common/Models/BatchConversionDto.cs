namespace VideoConverter.Common.Models;

public record BatchConversionDto(
    int BatchItemId,
    string BatchItemStatus,
    string InputPath,
    string? Reason,
    int? ConversionId,
    ConversionStatus? ConversionStatus,
    string? OutputDirectory,
    DateTime? ConversionCreatedAtUtc);
