using System.Globalization;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

/// <summary>
/// Property-based tests for VmafScoreAggregator.
/// Feature: vmaf-quality-scoring.
/// </summary>
public class VmafScoreAggregatorPropertyTests
{
    private readonly VmafScoreAggregator _aggregator = new();

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 6: VMAF score averaging correctness.
    /// Validates: Requirements 3.3.
    /// For any list of segment VMAF scores, the final VMAF score should equal
    /// the arithmetic mean of all segment scores.
    /// </summary>
    /// <returns>A property that validates VMAF score averaging correctness.</returns>
    [Property(MaxTest = 100)]
    public Property Aggregate_ShouldReturnArithmeticMean_ForValidScores()
    {
        // Generate lists of valid VMAF scores (0-100)
        var scoreGen = Gen.Choose(0, 10000).Select(i => i / 100.0);
        var scoresListGen = Gen.ListOf(scoreGen).Where(list => list.Count > 0);

        return Prop.ForAll(scoresListGen.ToArbitrary(), scores =>
        {
            var nullableScores = scores.Select(s => (double?)s).ToList();
            var result = _aggregator.Aggregate(nullableScores);

            var expectedMean = scores.Average();

            var isCorrect = result.HasValue && Math.Abs(result.Value - expectedMean) < 0.0001;

            return isCorrect
                .Label($"Scores=[{string.Join(", ", scores.Select(s => s.ToString("F2", CultureInfo.InvariantCulture)))}]")
                .Label($"Expected={expectedMean:F4}, Actual={result:F4}");
        });
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 6 (supplementary): Null scores are excluded from average.
    /// Validates: Requirements 3.3.
    /// For any list containing both valid scores and null values, the final score
    /// should be the arithmetic mean of only the non-null scores.
    /// </summary>
    /// <returns>A property that validates null score handling.</returns>
    [Property(MaxTest = 100)]
    public Property Aggregate_ShouldExcludeNullScores_FromAverage()
    {
        // Generate lists with a mix of valid scores and nulls
        var validScoreGen = Gen.Choose(0, 10000).Select(i => (double?)(i / 100.0));
        var nullGen = Gen.Constant<double?>(null);
        var mixedGen = Gen.OneOf(validScoreGen, nullGen);
        var mixedListGen = Gen.ListOf(mixedGen)
            .Where(list => list.Count > 0 && list.Any(s => s.HasValue));

        return Prop.ForAll(mixedListGen.ToArbitrary(), scores =>
        {
            var result = _aggregator.Aggregate(scores);

            var validScores = scores.Where(s => s.HasValue).Select(s => s!.Value).ToList();
            var expectedMean = validScores.Average();

            var isCorrect = result.HasValue && Math.Abs(result.Value - expectedMean) < 0.0001;

            return isCorrect
                .Label($"Scores=[{string.Join(", ", scores.Select(s => s?.ToString("F2", CultureInfo.InvariantCulture) ?? "null"))}]")
                .Label($"ValidCount={validScores.Count}, Expected={expectedMean:F4}, Actual={result:F4}");
        });
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 6 (supplementary): Empty or all-null lists return null.
    /// Validates: Requirements 3.3.
    /// For any empty list or list containing only null values, the result should be null.
    /// </summary>
    /// <returns>A property that validates empty/all-null handling.</returns>
    [Property(MaxTest = 100)]
    public Property Aggregate_ShouldReturnNull_ForEmptyOrAllNullScores()
    {
        // Generate lists of only null values
        var nullGen = Gen.Constant<double?>(null);
        var allNullListGen = Gen.ListOf(nullGen);

        return Prop.ForAll(allNullListGen.ToArbitrary(), scores =>
        {
            var result = _aggregator.Aggregate(scores);

            var isCorrect = result is null;

            return isCorrect
                .Label($"ScoreCount={scores.Count}, Result={result}");
        });
    }
}
