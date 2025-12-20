using System.ComponentModel.DataAnnotations;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

/// <summary>
/// Property-based tests for VmafOptions configuration validation.
/// Feature: vmaf-quality-scoring.
/// </summary>
public class VmafOptionsPropertyTests
{
    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 7: Configuration validation rejects invalid values.
    /// Validates: Requirements 4.4.
    /// For any VmafOptions with SegmentCount &lt;= 0 or SegmentDurationSeconds &lt;= 0 or TimeoutSeconds &lt;= 0,
    /// validation should fail with an appropriate error.
    /// </summary>
    /// <returns>A property that validates invalid configuration is rejected.</returns>
    [Property(MaxTest = 100)]
    public Property InvalidConfiguration_ShouldFailValidation()
    {
        // Generate invalid values (zero or negative)
        var invalidValueGen = Gen.Choose(-100, 0);
        var validValueGen = Gen.Choose(1, 1000);

        // Generate options where at least one value is invalid
        var invalidOptionsGen =
            from segmentCount in Gen.OneOf(invalidValueGen, validValueGen)
            from segmentDuration in Gen.OneOf(invalidValueGen, validValueGen)
            from timeout in Gen.OneOf(invalidValueGen, validValueGen)
            where segmentCount <= 0 || segmentDuration <= 0 || timeout <= 0
            select new VmafOptions
            {
                SegmentCount = segmentCount,
                SegmentDurationSeconds = segmentDuration,
                TimeoutSeconds = timeout,
            };

        return Prop.ForAll(invalidOptionsGen.ToArbitrary(), options =>
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(options);
            var isValid = Validator.TryValidateObject(options, context, validationResults, validateAllProperties: true);

            var hasInvalidSegmentCount = options.SegmentCount <= 0;
            var hasInvalidSegmentDuration = options.SegmentDurationSeconds <= 0;
            var hasInvalidTimeout = options.TimeoutSeconds <= 0;

            // Validation should fail when any value is invalid
            return (!isValid && validationResults.Count > 0)
                .Label($"SegmentCount={options.SegmentCount}, SegmentDuration={options.SegmentDurationSeconds}, Timeout={options.TimeoutSeconds}")
                .Label($"IsValid={isValid}, ValidationErrors={validationResults.Count}")
                .Label($"InvalidSegmentCount={hasInvalidSegmentCount}, InvalidDuration={hasInvalidSegmentDuration}, InvalidTimeout={hasInvalidTimeout}");
        });
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 7 (inverse): Valid configuration should pass validation.
    /// Validates: Requirements 4.4.
    /// For any VmafOptions with all positive values, validation should succeed.
    /// </summary>
    /// <returns>A property that validates valid configuration is accepted.</returns>
    [Property(MaxTest = 100)]
    public Property ValidConfiguration_ShouldPassValidation()
    {
        // Generate valid values (positive integers)
        var validValueGen = Gen.Choose(1, 10000);

        var validOptionsGen =
            from segmentCount in validValueGen
            from segmentDuration in validValueGen
            from timeout in validValueGen
            select new VmafOptions
            {
                SegmentCount = segmentCount,
                SegmentDurationSeconds = segmentDuration,
                TimeoutSeconds = timeout,
            };

        return Prop.ForAll(validOptionsGen.ToArbitrary(), options =>
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(options);
            var isValid = Validator.TryValidateObject(options, context, validationResults, validateAllProperties: true);

            return isValid
                .Label($"SegmentCount={options.SegmentCount}, SegmentDuration={options.SegmentDurationSeconds}, Timeout={options.TimeoutSeconds}")
                .Label($"IsValid={isValid}, ValidationErrors={string.Join(", ", validationResults.Select(r => r.ErrorMessage))}");
        });
    }
}
