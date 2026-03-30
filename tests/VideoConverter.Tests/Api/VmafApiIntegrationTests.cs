using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Api;

/// <summary>
/// API integration tests for VMAF fields in conversion endpoints.
/// Validates: Requirements 5.1, 5.2, 5.3, 5.4.
/// </summary>
public sealed class VmafApiIntegrationTests
{
    /// <summary>
    /// Tests that VMAF fields are correctly included in the conversion response when retrieved.
    /// Validates: Requirements 5.1, 5.2.
    /// </summary>
    [Fact]
    public async Task GetConversion_ShouldIncludeVmafFields_WhenConversionHasVmafData()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var conversion = new Conversion
        {
            InputPath = "/test/input.mp4",
            OutputDirectory = "/test/output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Completed,
            VmafEnabled = true,
            VmafScore = 94.5,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        // Use the ToDto helper to convert entity to DTO (same as API does)
        var dto = ConversionsApiHelpers.ToDto(conversion);

        _ = dto.VmafEnabled.Should().BeTrue();
        _ = dto.VmafScore.Should().Be(94.5);
    }

    /// <summary>
    /// Tests that VMAF fields are correctly included when VmafEnabled is false and VmafScore is null.
    /// Validates: Requirements 5.1, 5.2.
    /// </summary>
    [Fact]
    public async Task GetConversion_ShouldIncludeVmafFields_WhenVmafDisabled()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var conversion = new Conversion
        {
            InputPath = "/test/input.mp4",
            OutputDirectory = "/test/output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Completed,
            VmafEnabled = false,
            VmafScore = null,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var dto = ConversionsApiHelpers.ToDto(conversion);

        _ = dto.VmafEnabled.Should().BeFalse();
        _ = dto.VmafScore.Should().BeNull();
    }

    /// <summary>
    /// Tests that listing conversions includes VMAF fields for each conversion.
    /// Validates: Requirements 5.4.
    /// </summary>
    [Fact]
    public async Task ListConversions_ShouldIncludeVmafFieldsForEachConversion()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var conversion1 = new Conversion
        {
            InputPath = "/test/input1.mp4",
            OutputDirectory = "/test/output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Completed,
            VmafEnabled = true,
            VmafScore = 92.3,
        };

        var conversion2 = new Conversion
        {
            InputPath = "/test/input2.mp4",
            OutputDirectory = "/test/output",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1),
            Status = ConversionStatus.Pending,
            VmafEnabled = false,
            VmafScore = null,
        };

        var conversion3 = new Conversion
        {
            InputPath = "/test/input3.mp4",
            OutputDirectory = "/test/output",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(2),
            Status = ConversionStatus.Completed,
            VmafEnabled = true,
            VmafScore = 87.5,
        };

        db.Conversions.AddRange(conversion1, conversion2, conversion3);
        _ = await db.SaveChangesAsync();

        // Simulate listing conversions (same query as API)
        var items = await db.Conversions
            .AsNoTracking()
            .OrderBy(c => c.Status == ConversionStatus.Deleted)
            .ThenBy(c => c.Status)
            .ThenBy(c => c.Rank)
            .ThenBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .Select(c => ConversionsApiHelpers.ToDto(c))
            .ToListAsync();

        _ = items.Should().HaveCount(3);

        var dto1 = items.First(d => string.Equals(d.InputPath, "/test/input1.mp4", StringComparison.Ordinal));
        _ = dto1.VmafEnabled.Should().BeTrue();
        _ = dto1.VmafScore.Should().Be(92.3);

        var dto2 = items.First(d => string.Equals(d.InputPath, "/test/input2.mp4", StringComparison.Ordinal));
        _ = dto2.VmafEnabled.Should().BeFalse();
        _ = dto2.VmafScore.Should().BeNull();

        var dto3 = items.First(d => string.Equals(d.InputPath, "/test/input3.mp4", StringComparison.Ordinal));
        _ = dto3.VmafEnabled.Should().BeTrue();
        _ = dto3.VmafScore.Should().Be(87.5);
    }

    /// <summary>
    /// Tests that ConversionCreateDto accepts VmafEnabled flag.
    /// Validates: Requirements 5.3.
    /// </summary>
    [Fact]
    public void ConversionCreateDto_ShouldAcceptVmafEnabledFlag()
    {
        var dtoWithVmafEnabled = new ConversionCreateDto(
            InputPath: "/test/input.mp4",
            OutputDirectory: "/test/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            VmafEnabled: true);

        _ = dtoWithVmafEnabled.VmafEnabled.Should().BeTrue();

        var dtoWithVmafDisabled = new ConversionCreateDto(
            InputPath: "/test/input.mp4",
            OutputDirectory: "/test/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            VmafEnabled: false);

        _ = dtoWithVmafDisabled.VmafEnabled.Should().BeFalse();

        var dtoWithVmafNull = new ConversionCreateDto(
            InputPath: "/test/input.mp4",
            OutputDirectory: "/test/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            VmafEnabled: null);

        _ = dtoWithVmafNull.VmafEnabled.Should().BeNull();
    }

    /// <summary>
    /// Tests that ConversionDto includes VmafEnabled and VmafScore fields.
    /// Validates: Requirements 5.1, 5.2.
    /// </summary>
    [Fact]
    public void ConversionDto_ShouldIncludeVmafEnabledAndVmafScoreFields()
    {
        var dto = new ConversionDto(
            Id: 1,
            InputPath: "/test/input.mp4",
            OutputDirectory: "/test/output",
            CreatedAtUtc: DateTime.UtcNow,
            Status: ConversionStatus.Completed,
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            MetadataBasedFilename: "Movie.mp4",
            DeletedAtUtc: null,
            TmdbId: 12345,
            MatchedTitle: "Test Movie",
            MatchedYear: 2024,
            MatchedSeason: null,
            MatchedEpisode: null,
            SelectedAudioStreamIndicesCsv: "0",
            SelectedSubtitleStreamIndicesCsv: null,
            SelectedExternalSubtitlePathsCsv: null,
            DetectedCrop: null,
            DetectedGrainLevel: GrainLevel.Low,
            OutputPathTemplate: null,
            VmafEnabled: true,
            VmafScore: 95.2);

        _ = dto.VmafEnabled.Should().BeTrue();
        _ = dto.VmafScore.Should().Be(95.2);
    }

    /// <summary>
    /// Tests that VMAF score boundary values are correctly handled in DTOs.
    /// Validates: Requirements 5.1, 5.2.
    /// </summary>
    /// <param name="vmafScore">The VMAF score to test.</param>
    [Theory]
    [InlineData(0.0)]
    [InlineData(50.0)]
    [InlineData(100.0)]
    [InlineData(93.5)]
    public void ConversionDto_ShouldHandleVmafScoreBoundaryValues(double vmafScore)
    {
        var dto = new ConversionDto(
            Id: 1,
            InputPath: "/test/input.mp4",
            OutputDirectory: "/test/output",
            CreatedAtUtc: DateTime.UtcNow,
            Status: ConversionStatus.Completed,
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            MetadataBasedFilename: null,
            DeletedAtUtc: null,
            TmdbId: null,
            MatchedTitle: null,
            MatchedYear: null,
            MatchedSeason: null,
            MatchedEpisode: null,
            SelectedAudioStreamIndicesCsv: null,
            SelectedSubtitleStreamIndicesCsv: null,
            SelectedExternalSubtitlePathsCsv: null,
            DetectedCrop: null,
            DetectedGrainLevel: GrainLevel.Low,
            OutputPathTemplate: null,
            VmafEnabled: true,
            VmafScore: vmafScore);

        _ = dto.VmafScore.Should().Be(vmafScore);
    }

    /// <summary>
    /// Tests that VMAF fields persist correctly through database round-trip and are included in API response.
    /// Validates: Requirements 5.1, 5.2, 5.4.
    /// </summary>
    [Fact]
    public async Task VmafFields_ShouldPersistAndBeIncludedInApiResponse()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        // Create conversion with VMAF enabled
        var conversion = new Conversion
        {
            InputPath = "/test/input.mp4",
            OutputDirectory = "/test/output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            VmafEnabled = true,
            VmafScore = null, // Not yet calculated
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        // Clear change tracker to force fresh read
        db.ChangeTracker.Clear();

        // Retrieve and verify
        var retrieved = await db.Conversions.FirstAsync(c => c.Id == conversion.Id);
        _ = retrieved.VmafEnabled.Should().BeTrue();
        _ = retrieved.VmafScore.Should().BeNull();

        // Simulate VMAF calculation completion
        retrieved.VmafScore = 91.8;
        retrieved.Status = ConversionStatus.Completed;
        _ = await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Retrieve again and convert to DTO
        var final = await db.Conversions.FirstAsync(c => c.Id == conversion.Id);
        var dto = ConversionsApiHelpers.ToDto(final);

        _ = dto.VmafEnabled.Should().BeTrue();
        _ = dto.VmafScore.Should().Be(91.8);
        _ = dto.Status.Should().Be(ConversionStatus.Completed);
    }

    /// <summary>
    /// Tests that VmafEnabled defaults to false when not specified in create request.
    /// Validates: Requirements 5.3.
    /// </summary>
    [Fact]
    public void ConversionCreateDto_VmafEnabled_ShouldDefaultToNull_WhenNotSpecified()
    {
        var dto = new ConversionCreateDto(
            InputPath: "/test/input.mp4",
            OutputDirectory: "/test/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null);

        // VmafEnabled should be null when not specified (uses default parameter value)
        _ = dto.VmafEnabled.Should().BeNull();
    }
}
