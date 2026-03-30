using System.Reflection;

using FluentAssertions;

using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Services.Conversion;

public sealed class FfmpegGrainDetectorTests
{
    [Fact]
    public void Classify_ReturnsNone_WhenMedianPsnrExceedsLowThreshold()
    {
        var result = InvokeClassifier(45, 44, 38);

        result.Should().Be(GrainLevel.None);
    }

    [Fact]
    public void Classify_ReturnsLow_WhenMedianPsnrFallsBetweenThresholds()
    {
        var result = InvokeClassifier(41, 44, 38);

        result.Should().Be(GrainLevel.Low);
    }

    [Fact]
    public void Classify_ReturnsHigh_WhenMedianPsnrIsAtOrBelowHighThreshold()
    {
        var result = InvokeClassifier(38, 44, 38);

        result.Should().Be(GrainLevel.High);
    }

    [Fact]
    public void Classify_ReturnsLow_WhenMedianPsnrIsNaN()
    {
        var result = InvokeClassifier(double.NaN, 44, 38);

        result.Should().Be(GrainLevel.Low);
    }

    [Fact]
    public void ParseAveragePsnr_ReturnsParsedValue_WhenSummaryExists()
    {
        const string stderr = "[Parsed_psnr_0 @ 000] PSNR y:39.1 u:40.2 v:41.3 average:40.4567 min:38.9 max:42.0";

        var result = InvokePrivateStatic<double?>(typeof(FfmpegGrainDetector), "ParseAveragePsnr", stderr);

        result.Should().BeApproximately(40.4567, 0.0001);
    }

    [Fact]
    public void ParseAveragePsnr_ReturnsPositiveInfinity_WhenSummaryReportsInf()
    {
        const string stderr = "[Parsed_psnr_0 @ 000] PSNR y:inf u:inf v:inf average:inf min:inf max:inf";

        var result = InvokePrivateStatic<double?>(typeof(FfmpegGrainDetector), "ParseAveragePsnr", stderr);

        result.Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void ParseAveragePsnr_ReturnsNull_WhenSummaryReportsNaN()
    {
        const string stderr = "[Parsed_psnr_0 @ 000] PSNR y:nan u:nan v:nan average:nan min:nan max:nan";

        var result = InvokePrivateStatic<double?>(typeof(FfmpegGrainDetector), "ParseAveragePsnr", stderr);

        result.Should().BeNull();
    }

    [Fact]
    public void Selector_UsesLegacyProfileName_WhenPreferredProfileIsBlank()
    {
        var options = new FfmpegOptions
        {
            SvtParams = "film-grain=8:film-grain-denoise=0",
            SvtParamsHighGrain = string.Empty,
        };

        var profile = SvtAv1GrainProfileSelector.Select(options, GrainLevel.High);

        profile.AppliedProfileName.Should().Be("Legacy");
        profile.UsedFallback.Should().BeTrue();
        profile.SvtParams.Should().Be("film-grain=8:film-grain-denoise=0");
    }

    [Fact]
    public void BuildTimestamps_UsesExpectedSpacing_ForKnownDuration()
    {
        var result = InvokePrivateStatic<IEnumerable<double>>(typeof(FfmpegGrainDetector), "BuildTimestamps", 100d, 3)
            .ToArray();

        result.Should().Equal(2d, 50d, 98d);
    }

    [Fact]
    public void BuildPsnrArgs_UsesDenoiseDifferenceFilter()
    {
        var args = InvokePrivateStatic<List<string>>(typeof(FfmpegGrainDetector), "BuildPsnrArgs", "input.mkv", 12.5d, 1);

        args.Should().ContainInOrder("-hide_banner", "-loglevel", "info");
        args.Should().Contain("-filter_complex");
        args.Should().Contain("-map");

        var filterIndex = args.IndexOf("-filter_complex");
        filterIndex.Should().BeGreaterThanOrEqualTo(0);
        args[filterIndex + 1].Should().Contain("hqdn3d=");
        args[filterIndex + 1].Should().Contain("psnr=stats_file=-");
    }

    private static GrainLevel InvokeClassifier(double medianPsnr, double lowThreshold, double highThreshold)
    {
        return GrainDetectionClassifier.Classify(
            medianPsnr,
            lowThreshold,
            highThreshold);
    }

    private static T InvokePrivateStatic<T>(Type type, string methodName, params object?[] arguments)
    {
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Method '{methodName}' not found on '{type.FullName}'.");

        var result = method.Invoke(null, arguments);

        return result is null
            ? default!
            : (T)result;
    }
}
