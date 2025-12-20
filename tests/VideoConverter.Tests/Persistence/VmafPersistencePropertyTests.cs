using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Persistence;

/// <summary>
/// Property-based tests for VMAF field persistence.
/// Feature: vmaf-quality-scoring.
/// </summary>
public class VmafPersistencePropertyTests
{
    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 1: VMAF enabled flag persistence round-trip.
    /// Validates: Requirements 1.1, 5.1, 5.2.
    /// For any conversion create request with a VmafEnabled value, creating the conversion
    /// and then retrieving it should return the same VmafEnabled value that was specified.
    /// </summary>
    /// <returns>A property that validates VMAF enabled flag round-trips through persistence.</returns>
    [Property(MaxTest = 100)]
    public Property VmafEnabledFlag_ShouldRoundTripThroughPersistence()
    {
        var boolGen = Gen.Elements(true, false);

        return Prop.ForAll(boolGen.ToArbitrary(), vmafEnabled =>
        {
            using var context = DbContextTestFactory.CreateInMemory();

            var conversion = new Conversion
            {
                InputPath = "/test/input.mp4",
                OutputDirectory = "/test/output",
                CreatedAtUtc = DateTime.UtcNow,
                Status = ConversionStatus.Pending,
                VmafEnabled = vmafEnabled,
            };

            context.Conversions.Add(conversion);
            context.SaveChanges();

            // Clear the change tracker to force a fresh read from the database
            context.ChangeTracker.Clear();

            var retrieved = context.Conversions.First(c => c.Id == conversion.Id);

            return (retrieved.VmafEnabled == vmafEnabled)
                .Label($"Expected VmafEnabled={vmafEnabled}, got {retrieved.VmafEnabled}");
        });
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 2: VMAF score persistence round-trip.
    /// Validates: Requirements 2.1.
    /// For any valid VMAF score (0-100), storing it in a conversion entity
    /// and then retrieving the conversion should return the same VMAF score value.
    /// </summary>
    /// <returns>A property that validates VMAF score round-trips through persistence.</returns>
    [Property(MaxTest = 100)]
    public Property VmafScore_ShouldRoundTripThroughPersistence()
    {
        // Generate valid VMAF scores (0-100) or null
        var vmafScoreGen = Gen.OneOf(
            Gen.Constant<double?>(null),
            Gen.Choose(0, 10000).Select(i => (double?)i / 100.0));

        return Prop.ForAll(vmafScoreGen.ToArbitrary(), vmafScore =>
        {
            using var context = DbContextTestFactory.CreateInMemory();

            var conversion = new Conversion
            {
                InputPath = "/test/input.mp4",
                OutputDirectory = "/test/output",
                CreatedAtUtc = DateTime.UtcNow,
                Status = ConversionStatus.Completed,
                VmafEnabled = true,
                VmafScore = vmafScore,
            };

            context.Conversions.Add(conversion);
            context.SaveChanges();

            // Clear the change tracker to force a fresh read from the database
            context.ChangeTracker.Clear();

            var retrieved = context.Conversions.First(c => c.Id == conversion.Id);

            if (vmafScore is null)
            {
                return (retrieved.VmafScore is null)
                    .Label($"Expected VmafScore=null, got {retrieved.VmafScore}");
            }

            // Use approximate comparison for floating point
            var isEqual = retrieved.VmafScore.HasValue &&
                          Math.Abs(retrieved.VmafScore.Value - vmafScore.Value) < 0.0001;

            return isEqual
                .Label($"Expected VmafScore={vmafScore}, got {retrieved.VmafScore}");
        });
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 9: DTO includes all VMAF fields.
    /// Validates: Requirements 5.1, 5.2, 5.4.
    /// For any conversion entity with VmafEnabled and VmafScore values,
    /// the corresponding ConversionDto should contain both fields with matching values.
    /// </summary>
    /// <returns>A property that validates DTO includes all VMAF fields.</returns>
    [Property(MaxTest = 100)]
    public Property ConversionDto_ShouldIncludeAllVmafFields()
    {
        var boolGen = Gen.Elements(true, false);
        var vmafScoreGen = Gen.OneOf(
            Gen.Constant<double?>(null),
            Gen.Choose(0, 10000).Select(i => (double?)i / 100.0));

        var inputGen = from vmafEnabled in boolGen
                       from vmafScore in vmafScoreGen
                       select (VmafEnabled: vmafEnabled, VmafScore: vmafScore);

        return Prop.ForAll(inputGen.ToArbitrary(), input =>
        {
            var conversion = new Conversion
            {
                Id = 1,
                InputPath = "/test/input.mp4",
                OutputDirectory = "/test/output",
                CreatedAtUtc = DateTime.UtcNow,
                Status = ConversionStatus.Completed,
                VmafEnabled = input.VmafEnabled,
                VmafScore = input.VmafScore,
            };

            // Create DTO using the same mapping logic as the API
            var dto = new ConversionDto(
                conversion.Id,
                conversion.InputPath,
                conversion.OutputDirectory,
                conversion.CreatedAtUtc,
                conversion.Status,
                conversion.Crf,
                conversion.AudioBitrateKbps,
                conversion.AudioChannels,
                conversion.Resolution,
                conversion.MetadataBasedFilename,
                conversion.DeletedAtUtc,
                conversion.TmdbId,
                conversion.MatchedTitle,
                conversion.MatchedYear,
                conversion.MatchedSeason,
                conversion.MatchedEpisode,
                conversion.SelectedAudioStreamIndicesCsv,
                conversion.SelectedSubtitleStreamIndicesCsv,
                conversion.SelectedExternalSubtitlePathsCsv,
                conversion.DetectedCrop,
                conversion.OutputPathTemplate,
                conversion.VmafEnabled,
                conversion.VmafScore);

            var vmafEnabledMatches = dto.VmafEnabled == input.VmafEnabled;
            var vmafScoreMatches = input.VmafScore is null
                ? dto.VmafScore is null
                : dto.VmafScore.HasValue && Math.Abs(dto.VmafScore.Value - input.VmafScore.Value) < 0.0001;

            return (vmafEnabledMatches && vmafScoreMatches)
                .Label($"VmafEnabled: expected={input.VmafEnabled}, got={dto.VmafEnabled}")
                .Label($"VmafScore: expected={input.VmafScore}, got={dto.VmafScore}");
        });
    }
}
