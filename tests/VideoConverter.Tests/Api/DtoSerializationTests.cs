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
        var dto = new ConversionCreateDto("test.mp4", "output", 23, 128, null, 1080, ["English"], ["English"], null);
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<ConversionCreateDto>(json);
        _ = back.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void ConversionCreateDto_RoundTrips_WithEdgeValues()
    {
        var dto = new ConversionCreateDto(new string('a', 256), "output", 0, 1, AudioChannels.Stereo20, 720, [], [], null);
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
        var dto = new ConversionDto(2, "test.mp4", "output", DateTime.UtcNow, null, null, null, null, null);
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<ConversionDto>(json);
        _ = back.Should().BeEquivalentTo(dto, options => options.Excluding(x => x.CreatedAtUtc));
        _ = back!.CreatedAtUtc.Should().BeCloseTo(dto.CreatedAtUtc, TimeSpan.FromSeconds(2));
    }
}


