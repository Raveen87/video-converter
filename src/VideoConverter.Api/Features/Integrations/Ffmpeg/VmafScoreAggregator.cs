namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Aggregates multiple VMAF segment scores into a final score using arithmetic mean.
/// </summary>
public sealed class VmafScoreAggregator : IVmafScoreAggregator
{
    /// <inheritdoc />
    public double? Aggregate(IReadOnlyList<double?> segmentScores)
    {
        if (segmentScores is null or { Count: 0 })
        {
            return null;
        }

        var validScores = segmentScores.Where(s => s is not null).Select(s => s!.Value).ToList();

        if (validScores is { Count: 0 })
        {
            return null;
        }

        return validScores.Average();
    }
}
