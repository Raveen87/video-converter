using System.Text.Json;

using VideoConverter.Web.Models;

namespace VideoConverter.Web.Tests.Models;

public sealed class AudioChannelsConverterTests
{
    [Fact]
    public void Deserialize_WithStringValue_ReturnsString()
    {
        // Arrange - API returns string enum from System.Text.Json
        var json = """{"id":1,"inputPath":"test.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","audioChannels":"Stereo20"}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Stereo20", result.AudioChannels);
    }

    [Fact]
    public void Deserialize_WithNumericValue_ConvertsToString()
    {
        // Arrange - API might return numeric enum value
        var json = """{"id":1,"inputPath":"test.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","audioChannels":20}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Stereo20", result.AudioChannels);
    }

    [Fact]
    public void Deserialize_WithNumericValue51_ConvertsToSurround51()
    {
        // Arrange
        var json = """{"id":1,"inputPath":"test.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","audioChannels":51}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Surround51", result.AudioChannels);
    }

    [Fact]
    public void Deserialize_WithNullValue_ReturnsNull()
    {
        // Arrange
        var json = """{"id":1,"inputPath":"test.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","audioChannels":null}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
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
        var json = """{"id":1,"inputPath":"test.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z"}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.AudioChannels);
    }

    [Fact]
    public void Serialize_WithStringValue_WritesString()
    {
        // Arrange
        var dto = new ConversionDto(
            Id: 1,
            InputPath: "test.mp4",
            OutputDirectory: "out",
            CreatedAtUtc: DateTime.UtcNow,
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: "Stereo20",
            Resolution: null,
            MetadataBasedFilename: null);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
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
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            MetadataBasedFilename: null);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
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
        var json = """[{"id":1,"inputPath":"test1.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","audioChannels":"Stereo20"},{"id":2,"inputPath":"test2.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","audioChannels":51},{"id":3,"inputPath":"test3.mp4","outputDirectory":"out","createdAtUtc":"2024-01-01T00:00:00Z","audioChannels":null}]""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        // Act
        var results = JsonSerializer.Deserialize<ConversionDto[]>(json, options);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Length);
        Assert.Equal("Stereo20", results[0].AudioChannels);
        Assert.Equal("Surround51", results[1].AudioChannels);
        Assert.Null(results[2].AudioChannels);
    }
}
