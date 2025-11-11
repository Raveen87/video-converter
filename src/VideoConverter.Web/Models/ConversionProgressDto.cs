namespace VideoConverter.Web.Models;

public record ConversionProgressDto(
    int ConversionId,
    string Status,
    double? PercentageComplete,
    string? CurrentFrame,
    string? CurrentFps,
    string? CurrentBitrate,
    TimeSpan? Elapsed,
    TimeSpan? Remaining);
