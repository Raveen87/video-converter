using System.Text.Json;
using FluentAssertions;
using VideoConverter.Web.Dtos;

namespace VideoConverter.Tests.Api;

public class ApiLayerTests
{
    // Note: Integration tests with WebApplicationFactory would require a non-static Program class
    // For now, we'll focus on unit testing the API logic through the service layer
    // In a real scenario, you would refactor Program.cs to have a non-static Main method
    // or create a separate Startup class for integration testing

    [Fact]
    public void ConversionCreateDto_WithValidData_SerializesCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: null,
            Resolution: 1080,
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<ConversionCreateDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.InputPath.Should().Be("test.mp4");
        _ = deserialized.OutputDirectory.Should().Be("output");
        _ = deserialized.Crf.Should().Be(23);
        _ = deserialized.AudioBitrateKbps.Should().Be(128);
        _ = deserialized.Resolution.Should().Be(1080);
        _ = deserialized.AudioLanguages.Should().BeEquivalentTo("English");
        _ = deserialized.SubtitleLanguages.Should().BeEquivalentTo("English");
    }

    [Fact]
    public void ConversionCreateDto_WithNullValues_SerializesCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null);

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<ConversionCreateDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.InputPath.Should().Be("test.mp4");
        _ = deserialized.OutputDirectory.Should().Be("output");
        _ = deserialized.Crf.Should().BeNull();
        _ = deserialized.AudioBitrateKbps.Should().BeNull();
        _ = deserialized.Resolution.Should().BeNull();
        _ = deserialized.AudioLanguages.Should().BeNull();
        _ = deserialized.SubtitleLanguages.Should().BeNull();
    }

    [Fact]
    public void ConversionCreateDto_WithEmptyLists_SerializesCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: null,
            Resolution: 1080,
            AudioLanguages: [],
            SubtitleLanguages: []);

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<ConversionCreateDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.AudioLanguages.Should().BeEquivalentTo();
        _ = deserialized.SubtitleLanguages.Should().BeEquivalentTo();
    }

    [Fact]
    public void ConversionDto_WithValidData_SerializesCorrectly()
    {
        // Arrange
        var conversion = new ConversionDto(
            Id: 1,
            InputPath: "test.mp4",
            OutputDirectory: "output",
            CreatedAtUtc: DateTime.UtcNow,
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: null,
            Resolution: 1080,
            MetadataBasedFilename: "Test Movie.2020.mp4");

        // Act
        var json = JsonSerializer.Serialize(conversion);
        var deserialized = JsonSerializer.Deserialize<ConversionDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.Id.Should().Be(1);
        _ = deserialized.InputPath.Should().Be("test.mp4");
        _ = deserialized.OutputDirectory.Should().Be("output");
        _ = deserialized.Crf.Should().Be(23);
        _ = deserialized.AudioBitrateKbps.Should().Be(128);
        _ = deserialized.Resolution.Should().Be(1080);
        _ = deserialized.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ConversionDto_WithNullValues_SerializesCorrectly()
    {
        // Arrange
        var conversion = new ConversionDto(
            Id: 1,
            InputPath: "test.mp4",
            OutputDirectory: "output",
            CreatedAtUtc: DateTime.UtcNow,
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            MetadataBasedFilename: null);

        // Act
        var json = JsonSerializer.Serialize(conversion);
        var deserialized = JsonSerializer.Deserialize<ConversionDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.Id.Should().Be(1);
        _ = deserialized.InputPath.Should().Be("test.mp4");
        _ = deserialized.OutputDirectory.Should().Be("output");
        _ = deserialized.Crf.Should().BeNull();
        _ = deserialized.AudioBitrateKbps.Should().BeNull();
        _ = deserialized.Resolution.Should().BeNull();
    }

    [Fact]
    public void ConversionCreateDto_WithSpecialCharacters_SerializesCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test with spaces & special chars.mp4",
            OutputDirectory: "output with spaces",
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: null,
            Resolution: 1080,
            AudioLanguages: ["English", "Français", "Español"],
            SubtitleLanguages: ["English", "中文", "العربية"]);

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<ConversionCreateDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.InputPath.Should().Be("test with spaces & special chars.mp4");
        _ = deserialized.OutputDirectory.Should().Be("output with spaces");
        _ = deserialized.AudioLanguages.Should().BeEquivalentTo("English", "Français", "Español");
        _ = deserialized.SubtitleLanguages.Should().BeEquivalentTo("English", "中文", "العربية");
    }

    [Fact]
    public void ConversionCreateDto_WithVeryLongStrings_SerializesCorrectly()
    {
        // Arrange
        var longPath = new string('a', 1000);
        var longLanguages = Enumerable.Range(1, 100).Select(i => $"Language{i}").ToArray();

        var request = new ConversionCreateDto(
            InputPath: longPath,
            OutputDirectory: "output",
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: null,
            Resolution: 1080,
            AudioLanguages: longLanguages,
            SubtitleLanguages: longLanguages);

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<ConversionCreateDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.InputPath.Should().Be(longPath);
        _ = deserialized.AudioLanguages.Should().BeEquivalentTo(longLanguages);
        _ = deserialized.SubtitleLanguages.Should().BeEquivalentTo(longLanguages);
    }

    [Fact]
    public void ConversionCreateDto_WithBoundaryValues_SerializesCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: 0, // Minimum valid CRF
            AudioBitrateKbps: 1, // Minimum valid bitrate
            AudioChannels: null,
            Resolution: 720, // Minimum valid resolution
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<ConversionCreateDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.Crf.Should().Be(0);
        _ = deserialized.AudioBitrateKbps.Should().Be(1);
        _ = deserialized.Resolution.Should().Be(720);
    }

    [Fact]
    public void ConversionCreateDto_WithMaximumValues_SerializesCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: 63, // Maximum valid CRF
            AudioBitrateKbps: 450, // Maximum valid bitrate
            AudioChannels: null,
            Resolution: 2160, // Maximum valid resolution
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<ConversionCreateDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.Crf.Should().Be(63);
        _ = deserialized.AudioBitrateKbps.Should().Be(450);
        _ = deserialized.Resolution.Should().Be(2160);
    }

    [Fact]
    public void ConversionCreateDto_WithInvalidValues_SerializesCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: 100, // Invalid CRF
            AudioBitrateKbps: 500, // Invalid bitrate
            AudioChannels: null,
            Resolution: 1440, // Invalid resolution
            AudioLanguages: [""], // Invalid language
            SubtitleLanguages: ["   "]); // Invalid language

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<ConversionCreateDto>(json);

        // Assert
        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.Crf.Should().Be(100); // Should preserve invalid values
        _ = deserialized.AudioBitrateKbps.Should().Be(500);
        _ = deserialized.Resolution.Should().Be(1440);
        _ = deserialized.AudioLanguages.Should().BeEquivalentTo("");
        _ = deserialized.SubtitleLanguages.Should().BeEquivalentTo("   ");
    }
}
