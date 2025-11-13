using System.Text.Json.Serialization;

namespace VideoConverter.Web.Models;

public record ConversionProgressDto(
    [property: JsonConverter(typeof(ConversionStatusConverter))]
    string Status,
    double? PercentageComplete,
    double? CurrentTimeSeconds,
    double? TotalDurationSeconds,
    TimeSpan? EstimatedTimeRemaining);
