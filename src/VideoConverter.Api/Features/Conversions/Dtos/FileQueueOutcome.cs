namespace VideoConverter.Api.Features.Conversions.Dtos;

public record FileQueueOutcome(
    string InputPath,
    string Status,
    string? Reason,
    int? ConversionId);
