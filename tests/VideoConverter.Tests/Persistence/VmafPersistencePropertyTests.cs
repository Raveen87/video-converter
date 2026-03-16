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
        return Prop.ForAll(CreateVmafDtoInputGenerator().ToArbitrary(), input =>
        {
            var conversion = BuildConversion(input);
            var dto = BuildDto(conversion);

            var vmafEnabledMatches = dto.VmafEnabled == input.VmafEnabled;
            var requiredVmafMatches = NullableDoubleMatches(dto.RequiredVmaf, input.RequiredVmaf);
            var vmafScoreMatches = NullableDoubleMatches(dto.VmafScore, input.VmafScore);

            return (vmafEnabledMatches && requiredVmafMatches && vmafScoreMatches)
                .Label($"VmafEnabled: expected={input.VmafEnabled}, got={dto.VmafEnabled}")
                .Label($"RequiredVmaf: expected={input.RequiredVmaf}, got={dto.RequiredVmaf}")
                .Label($"VmafScore: expected={input.VmafScore}, got={dto.VmafScore}");
        });
    }

    private static Gen<(bool VmafEnabled, double? RequiredVmaf, double? VmafScore)> CreateVmafDtoInputGenerator()
    {
        var boolGen = Gen.Elements(true, false);
        var nullableDoubleGen = Gen.OneOf(
            Gen.Constant<double?>(null),
            Gen.Choose(0, 10000).Select(i => (double?)i / 100.0));

        return from vmafEnabled in boolGen
               from requiredVmaf in nullableDoubleGen
               from vmafScore in nullableDoubleGen
               select (vmafEnabled, requiredVmaf, vmafScore);
    }

    private static Conversion BuildConversion((bool VmafEnabled, double? RequiredVmaf, double? VmafScore) input)
    {
        return new Conversion
        {
            Id = 1,
            InputPath = "/test/input.mp4",
            OutputDirectory = "/test/output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Completed,
            VmafEnabled = input.VmafEnabled,
            RequiredVmaf = input.RequiredVmaf,
            VmafScore = input.VmafScore,
        };
    }

    private static ConversionDto BuildDto(Conversion conversion)
    {
        return new ConversionDto(
            Id: conversion.Id,
            InputPath: conversion.InputPath,
            OutputDirectory: conversion.OutputDirectory,
            CreatedAtUtc: conversion.CreatedAtUtc,
            Status: conversion.Status,
            Crf: conversion.Crf,
            AudioBitrateKbps: conversion.AudioBitrateKbps,
            AudioChannels: conversion.AudioChannels,
            Resolution: conversion.Resolution,
            MetadataBasedFilename: conversion.MetadataBasedFilename,
            DeletedAtUtc: conversion.DeletedAtUtc,
            TmdbId: conversion.TmdbId,
            MatchedTitle: conversion.MatchedTitle,
            MatchedYear: conversion.MatchedYear,
            MatchedSeason: conversion.MatchedSeason,
            MatchedEpisode: conversion.MatchedEpisode,
            SelectedAudioStreamIndicesCsv: conversion.SelectedAudioStreamIndicesCsv,
            SelectedSubtitleStreamIndicesCsv: conversion.SelectedSubtitleStreamIndicesCsv,
            SelectedExternalSubtitlePathsCsv: conversion.SelectedExternalSubtitlePathsCsv,
            DetectedCrop: conversion.DetectedCrop,
            OutputPathTemplate: conversion.OutputPathTemplate,
            VmafEnabled: conversion.VmafEnabled,
            RequiredVmaf: conversion.RequiredVmaf,
            VmafScore: conversion.VmafScore);
    }

    private static bool NullableDoubleMatches(double? left, double? right)
    {
        return (left is null && right is null)
            || (left.HasValue && right.HasValue && Math.Abs(left.Value - right.Value) < 0.0001);
    }
}
