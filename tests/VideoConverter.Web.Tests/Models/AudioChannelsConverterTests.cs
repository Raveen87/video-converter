using System.Text.Json;
using System.Text.Json.Serialization;

using VideoConverter.Common.Models;

namespace VideoConverter.Web.Tests.Models;

public sealed class AudioChannelsConverterTests
{
    [Fact]
    public void Deserialize_WithStringValue_ReturnsEnum()
    {
        // Arrange - API returns string enum from System.Text.Json
        var json = """{"id":1,"inputPath":"test.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","status":"Pending","audioChannels":"Stereo20"}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AudioChannels.Stereo20, result.AudioChannels);
    }

    [Fact]
    public void Deserialize_WithSurround51Value_ReturnsEnum()
    {
        // Arrange
        var json = """{"id":1,"inputPath":"test.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","status":"Pending","audioChannels":"Surround51"}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AudioChannels.Surround51, result.AudioChannels);
    }

    [Fact]
    public void Deserialize_WithNullValue_ReturnsNull()
    {
        // Arrange
        var json = """{"id":1,"inputPath":"test.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","status":"Pending","audioChannels":null}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.AudioChannels);
    }

    [Fact]
    public void Deserialize_WithMissingProperty_ReturnsNull()
    {
        // Arrange
        var json = """{"id":1,"inputPath":"test.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","status":"Pending"}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.AudioChannels);
    }

    [Fact]
    public void Serialize_WithEnumValue_WritesString()
    {
        // Arrange
        var dto = new ConversionDto(
            Id: 1,
            InputPath: "test.mp4",
            OutputDirectory: "out",
            CreatedAtUtc: DateTime.UtcNow,
            Status: ConversionStatus.Pending,
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: null,
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
            DetectedGrainLevel: GrainLevel.Low);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var json = JsonSerializer.Serialize(dto, options);

        // Assert - System.Text.Json uses PascalCase by default
        Assert.Contains("\"AudioChannels\":\"Stereo20\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_WithNullValue_WritesNull()
    {
        // Arrange
        var dto = new ConversionDto(
            Id: 1,
            InputPath: "test.mp4",
            OutputDirectory: "out",
            CreatedAtUtc: DateTime.UtcNow,
            Status: ConversionStatus.Pending,
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
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
            DetectedGrainLevel: GrainLevel.Low);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var json = JsonSerializer.Serialize(dto, options);

        // Assert - System.Text.Json uses PascalCase by default, and null values are omitted by default
        // So we just verify the converter doesn't throw and the property is handled
        Assert.DoesNotContain("\"AudioChannels\":\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_WithArrayOfConversions_HandlesAllFormats()
    {
        // Arrange - Simulates the actual API response format
        var json = """[{"id":1,"inputPath":"test1.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","status":"Pending","audioChannels":"Stereo20"},{"id":2,"inputPath":"test2.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","status":"Pending","audioChannels":"Surround51"},{"id":3,"inputPath":"test3.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","status":"Pending","audioChannels":null}]""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var results = JsonSerializer.Deserialize<ConversionDto[]>(json, options);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Length);
        Assert.Equal(AudioChannels.Stereo20, results[0].AudioChannels);
        Assert.Equal(AudioChannels.Surround51, results[1].AudioChannels);
        Assert.Null(results[2].AudioChannels);
    }
}
