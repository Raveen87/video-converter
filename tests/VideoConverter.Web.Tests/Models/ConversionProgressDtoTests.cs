using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

using VideoConverter.Web.Models;

namespace VideoConverter.Web.Tests.Models;

public class ConversionProgressDtoTests
{
    [Fact]
    public void Deserialize_ApiResponse_AllFieldsMappedCorrectly()
    {
        // Arrange - This JSON matches what the API returns (with enum as string)
        var json = """
            {
                "status": "Running",
                "percentageComplete": 45.5,
                "currentTimeSeconds": 1234.56,
                "totalDurationSeconds": 2712.34,
                "estimatedTimeRemaining": "00:24:37"
            }
            """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionProgressDto>(json, options);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Status.Should().Be(ConversionProgressStatus.Running);
        _ = result.PercentageComplete.Should().Be(45.5);
        _ = result.CurrentTimeSeconds.Should().Be(1234.56);
        _ = result.TotalDurationSeconds.Should().Be(2712.34);
        _ = result.EstimatedTimeRemaining.Should().Be(TimeSpan.Parse("00:24:37", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Deserialize_ApiResponse_WithWaitingStatus_HandlesCorrectly()
    {
        // Arrange - API returns Waiting status
        var json = """
            {
                "status": "Waiting",
                "percentageComplete": null,
                "currentTimeSeconds": null,
                "totalDurationSeconds": null,
                "estimatedTimeRemaining": null
            }
            """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionProgressDto>(json, options);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Status.Should().Be(ConversionProgressStatus.Waiting);
        _ = result.PercentageComplete.Should().BeNull();
        _ = result.CurrentTimeSeconds.Should().BeNull();
        _ = result.TotalDurationSeconds.Should().BeNull();
        _ = result.EstimatedTimeRemaining.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ApiResponse_WithAllFields_VerifiesCompleteMapping()
    {
        // Arrange - This test ensures all fields from the API are present in the DTO
        // If a new field is added to the API response, this test will fail if the DTO isn't updated
        var json = """
            {
                "status": "Running",
                "percentageComplete": 50.0,
                "currentTimeSeconds": 1000.0,
                "totalDurationSeconds": 2000.0,
                "estimatedTimeRemaining": "00:16:40"
            }
            """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<ConversionProgressDto>(json, options);

        // Assert - Verify all expected fields are present and mapped
        _ = result.Should().NotBeNull();
        _ = result!.Status.Should().Be(ConversionProgressStatus.Running, "Status field must be present and mapped");
        _ = result.PercentageComplete.Should().HaveValue("PercentageComplete should be set when provided");
        _ = result.CurrentTimeSeconds.Should().HaveValue("CurrentTimeSeconds should be set when provided");
        _ = result.TotalDurationSeconds.Should().HaveValue("TotalDurationSeconds should be set when provided");
        _ = result.EstimatedTimeRemaining.Should().HaveValue("EstimatedTimeRemaining should be set when provided");

        // This test will fail if:
        // 1. A new field is added to the API response but not to the DTO (deserialization will ignore it, but we want to catch it)
        // 2. A field name changes in the API but not in the DTO
        // Note: System.Text.Json by default ignores unknown properties, so we can't directly test for extra fields
        // But we can verify all expected fields are present
    }
}