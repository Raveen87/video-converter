using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

/// <summary>
/// Unit tests for VmafOptions configuration.
/// Feature: vmaf-quality-scoring.
/// </summary>
public sealed class VmafOptionsTests
{
    /// <summary>
    /// Verifies that VmafOptions has sensible default values.
    /// Validates: Requirements 4.3.
    /// </summary>
    [Fact]
    public void DefaultValues_AreSensible()
    {
        var options = new VmafOptions();

        Assert.Equal(5, options.SegmentCount);
        Assert.Equal(10, options.SegmentDurationSeconds);
        Assert.Equal(300, options.TimeoutSeconds);
    }

    /// <summary>
    /// Verifies that VmafOptions.SectionName is correct for configuration binding.
    /// </summary>
    [Fact]
    public void SectionName_IsVmaf()
    {
        Assert.Equal("Vmaf", VmafOptions.SectionName);
    }
}
