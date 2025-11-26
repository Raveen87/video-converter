using System.Text.Json;
using FluentAssertions;
using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Tests.TestUtils.Builders;

namespace VideoConverter.Tests.Api;

public class DtoSerializationTests
{
    [Fact]
    public void ConversionCreateDto_RoundTrips_WithTypicalValues()
    {
        var dto = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithCrf(23)
            .WithAudioBitrateKbps(128)
            .WithResolution(1080)
            .WithAudioLanguages("English")
            .WithSubtitleLanguages("English")
            .Build();
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<ConversionCreateDto>(json);
        _ = back.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void ConversionCreateDto_RoundTrips_WithEdgeValues()
    {
        var dto = new ConversionCreateDtoBuilder()
            .WithInputPath(new string('a', 256))
            .WithOutputDirectory("output")
            .WithCrf(0)
            .WithAudioBitrateKbps(1)
            .WithAudioChannels(AudioChannels.Stereo20)
            .WithResolution(720)
            .WithAudioLanguages()
            .WithSubtitleLanguages()
            .Build();
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<ConversionCreateDto>(json);
        _ = back.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void ConversionDto_RoundTrips_WithValues()
    {
        var dto = new ConversionDto(1, "test.mp4", "output", DateTime.UtcNow, ConversionStatus.Pending, 23, 128, AudioChannels.Stereo20, 1080, "Name.mp4", null);
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<ConversionDto>(json);
        _ = back.Should().BeEquivalentTo(dto, options => options.Excluding(x => x.CreatedAtUtc));
        _ = back!.CreatedAtUtc.Should().BeCloseTo(dto.CreatedAtUtc, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ConversionDto_RoundTrips_WithNullables()
    {
        var dto = new ConversionDto(2, "test.mp4", "output", DateTime.UtcNow, ConversionStatus.Pending, Crf: 30, AudioBitrateKbps: null, Resolution: null, MetadataBasedFilename: null, AudioChannels: null, DeletedAtUtc: null);
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<ConversionDto>(json);
        _ = back.Should().BeEquivalentTo(dto, options => options.Excluding(x => x.CreatedAtUtc));
        _ = back!.CreatedAtUtc.Should().BeCloseTo(dto.CreatedAtUtc, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DirectoryQueueRequestDto_RoundTrips_WithValues()
    {
        var dto = new DirectoryQueueRequestDto(
            DirectoryPath: "C:/media",
            OutputDirectory: "C:/out",
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            AudioLanguages: new[] { "English" },
            SubtitleLanguages: new[] { "English" },
            OutputFilenameTemplate: "{Title}",
            Extensions: new[] { ".mkv", ".mp4" });

        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<DirectoryQueueRequestDto>(json);
        _ = back.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void DirectoryQueueResultDto_RoundTrips_WithValues()
    {
        var outcomes = new[]
        {
            new FileQueueOutcome("file1.mkv", "queued", null, 1),
            new FileQueueOutcome("file2.mp4", "skipped", "duplicate", null),
        };
        var dto = new DirectoryQueueResultDto(
            TotalFilesFound: 3,
            Queued: 1,
            SkippedDuplicates: 1,
            Invalid: 1,
            Outcomes: outcomes);

        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<DirectoryQueueResultDto>(json);
        _ = back.Should().BeEquivalentTo(dto);
    }
}
