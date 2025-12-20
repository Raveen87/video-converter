using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

/// <summary>
/// Unit tests for VmafOutputParser.
/// Feature: vmaf-quality-scoring.
/// </summary>
public sealed class VmafOutputParserTests
{
    private readonly VmafOutputParser _parser = new(NullLogger<VmafOutputParser>.Instance);

    /// <summary>
    /// Verifies that valid libvmaf JSON output with pooled_metrics.vmaf.mean is parsed correctly.
    /// Validates: Requirements 2.1.
    /// </summary>
    [Fact]
    public void Parse_ValidJsonWithPooledMetricsMean_ReturnsScore()
    {
        var json = """
            {
                "version": "2.3.1",
                "pooled_metrics": {
                    "vmaf": {
                        "min": 85.123,
                        "max": 98.456,
                        "mean": 94.567,
                        "harmonic_mean": 93.210
                    }
                }
            }
            """;

        var result = _parser.Parse(json);

        result.Should().BeApproximately(94.567, 0.001);
    }

    /// <summary>
    /// Verifies that valid libvmaf JSON output with only harmonic_mean falls back correctly.
    /// Validates: Requirements 2.1.
    /// </summary>
    [Fact]
    public void Parse_ValidJsonWithOnlyHarmonicMean_ReturnsHarmonicMean()
    {
        var json = """
            {
                "pooled_metrics": {
                    "vmaf": {
                        "harmonic_mean": 91.234
                    }
                }
            }
            """;

        var result = _parser.Parse(json);

        result.Should().BeApproximately(91.234, 0.001);
    }

    /// <summary>
    /// Verifies that older libvmaf format with root-level "VMAF score" is parsed correctly.
    /// Validates: Requirements 2.1.
    /// </summary>
    [Fact]
    public void Parse_ValidJsonWithRootVmafScore_ReturnsScore()
    {
        var json = """
            {
                "VMAF score": 88.765
            }
            """;

        var result = _parser.Parse(json);

        result.Should().BeApproximately(88.765, 0.001);
    }

    /// <summary>
    /// Verifies that JSON embedded in FFmpeg output is extracted and parsed.
    /// Validates: Requirements 2.1.
    /// </summary>
    [Fact]
    public void Parse_JsonEmbeddedInFfmpegOutput_ReturnsScore()
    {
        var output = """
            frame=  100 fps=25.0 q=28.0 size=    1024kB time=00:00:04.00 bitrate=2097.2kbits/s speed=1.00x
            {"pooled_metrics": {"vmaf": {"mean": 92.5}}}
            video:1024kB audio:0kB subtitle:0kB other streams:0kB global headers:0kB muxing overhead: unknown
            """;

        var result = _parser.Parse(output);

        result.Should().BeApproximately(92.5, 0.001);
    }

    /// <summary>
    /// Verifies that empty input returns null.
    /// Validates: Requirements 2.4.
    /// </summary>
    /// <param name="input">The input string to test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespaceInput_ReturnsNull(string? input)
    {
        var result = _parser.Parse(input!);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that output without JSON returns null.
    /// Validates: Requirements 2.4.
    /// </summary>
    [Fact]
    public void Parse_NoJsonInOutput_ReturnsNull()
    {
        var output = "frame=  100 fps=25.0 q=28.0 size=    1024kB time=00:00:04.00";

        var result = _parser.Parse(output);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that malformed JSON returns null.
    /// Validates: Requirements 2.4.
    /// </summary>
    [Fact]
    public void Parse_MalformedJson_ReturnsNull()
    {
        var output = "{\"pooled_metrics\": {\"vmaf\": {\"mean\":";

        var result = _parser.Parse(output);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that JSON without VMAF score returns null.
    /// Validates: Requirements 2.4.
    /// </summary>
    [Fact]
    public void Parse_JsonWithoutVmafScore_ReturnsNull()
    {
        var json = """
            {
                "pooled_metrics": {
                    "psnr": {
                        "mean": 45.6
                    }
                }
            }
            """;

        var result = _parser.Parse(json);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that JSON with non-numeric VMAF score returns null.
    /// Validates: Requirements 2.4.
    /// </summary>
    [Fact]
    public void Parse_NonNumericVmafScore_ReturnsNull()
    {
        var json = """
            {
                "pooled_metrics": {
                    "vmaf": {
                        "mean": "not a number"
                    }
                }
            }
            """;

        var result = _parser.Parse(json);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that print_summary text output with "VMAF score:" format is parsed correctly.
    /// This is the cross-platform format that works on Windows.
    /// Validates: Requirements 2.1.
    /// </summary>
    [Fact]
    public void Parse_PrintSummaryWithColonFormat_ReturnsScore()
    {
        var output = """
            [libvmaf @ 0000019275ac3c40] VMAF score: 92.123456
            frame=  100 fps=25.0 q=28.0 size=    1024kB
            """;

        var result = _parser.Parse(output);

        result.Should().BeApproximately(92.123456, 0.000001);
    }

    /// <summary>
    /// Verifies that print_summary text output with "VMAF score =" format is parsed correctly.
    /// Validates: Requirements 2.1.
    /// </summary>
    [Fact]
    public void Parse_PrintSummaryWithEqualsFormat_ReturnsScore()
    {
        var output = """
            [libvmaf @ 0x12345] VMAF score = 88.5
            """;

        var result = _parser.Parse(output);

        result.Should().BeApproximately(88.5, 0.001);
    }

    /// <summary>
    /// Verifies that print_summary text output with integer score is parsed correctly.
    /// Validates: Requirements 2.1.
    /// </summary>
    [Fact]
    public void Parse_PrintSummaryWithIntegerScore_ReturnsScore()
    {
        var output = "[libvmaf] VMAF score: 95";

        var result = _parser.Parse(output);

        result.Should().Be(95.0);
    }

    /// <summary>
    /// Verifies that text parsing is case-insensitive.
    /// Validates: Requirements 2.1.
    /// </summary>
    [Fact]
    public void Parse_PrintSummaryCaseInsensitive_ReturnsScore()
    {
        var output = "[libvmaf] vmaf SCORE: 91.5";

        var result = _parser.Parse(output);

        result.Should().BeApproximately(91.5, 0.001);
    }

    /// <summary>
    /// Verifies that text output is preferred over JSON when both are present.
    /// Validates: Requirements 2.1.
    /// </summary>
    [Fact]
    public void Parse_BothTextAndJsonPresent_PrefersTextOutput()
    {
        var output = """
            [libvmaf] VMAF score: 92.0
            {"pooled_metrics": {"vmaf": {"mean": 91.0}}}
            """;

        var result = _parser.Parse(output);

        // Text output (92.0) should be preferred over JSON (91.0)
        result.Should().BeApproximately(92.0, 0.001);
    }
}
