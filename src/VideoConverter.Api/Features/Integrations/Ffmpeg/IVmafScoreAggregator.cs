namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Aggregates multiple VMAF segment scores into a final score.
/// </summary>
public interface IVmafScoreAggregator
{
    /// <summary>
    /// Calculates the final VMAF score from multiple segment scores.
    /// </summary>
    /// <param name="segmentScores">The VMAF scores from individual segments. May contain null values for failed segments.</param>
    /// <returns>
    /// The arithmetic mean of all non-null segment scores, or null if no valid scores are available.
    /// </returns>
    double? Aggregate(IReadOnlyList<double?> segmentScores);
}
