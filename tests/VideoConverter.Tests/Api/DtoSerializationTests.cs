using System.Text.Json;

using FluentAssertions;

using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.Api;

public class DtoSerializationTests
{
    [Fact]
    public void ConversionCreateDto_RoundTrips_WithTypicalValues()
    {
        var dto = new ConversionCreateDto("test.mp4", "output", 23, 128, AudioChannels: null, 1080, ["English"], ["English"], ProfileId: null);
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<ConversionCreateDto>(json);
        _ = back.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void ConversionCreateDto_RoundTrips_WithEdgeValues()
    {
        var dto = new ConversionCreateDto(new string('a', 256), "output", 0, 1, AudioChannels.Stereo20, 720, [], [], ProfileId: null);
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<ConversionCreateDto>(json);
        _ = back.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void ConversionDto_RoundTrips_WithValues()
    {
        var dto = new ConversionDto(1, "test.mp4", "output", DateTime.UtcNow, 23, 128, AudioChannels.Stereo20, 1080, "Name.mp4");
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<ConversionDto>(json);
        _ = back.Should().BeEquivalentTo(dto, options => options.Excluding(x => x.CreatedAtUtc));
        _ = back!.CreatedAtUtc.Should().BeCloseTo(dto.CreatedAtUtc, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ConversionDto_RoundTrips_WithNullables()
    {
        var dto = new ConversionDto(2, "test.mp4", "output", DateTime.UtcNow, Crf: 30, AudioBitrateKbps: null, Resolution: null, MetadataBasedFilename: null, AudioChannels: null);
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
            ProfileId: 1,
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
