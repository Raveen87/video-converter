using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using ConversionEntity = VideoConverter.Api.Shared.Domain.Entities.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

public sealed class CrfOptimizationServiceTests
{
    [Fact]
    public async Task OptimizeCrfAsync_UsesConfiguredCodecArgsForOptimizationClip()
    {
        var processManager = new RecordingProcessManager();
        var segmentCalculator = new FixedSegmentCalculator([0]);
        var mediaProber = new FixedMediaProber();
        var parser = new DelegatingVmafParser(_ => 100);

        var service = CreateService(processManager, segmentCalculator, mediaProber, parser, new FfmpegOptions
        {
            VideoCodec = "libx265",
            PixelFormat = "yuv420p",
            Preset = "slow",
            Tune = "grain",
            Gop = "150",
            SvtParams = "ignored-for-x265",
        });

        var conversion = CreateConversion();
        _ = await service.OptimizeCrfAsync(conversion, requiredVmaf: 90, startCrf: 30, CancellationToken.None);

        var encodeArgs = processManager.Requests
            .First(r => !r.Arguments.Contains("-lavfi", StringComparer.Ordinal))
            .Arguments;

        AssertContainsPair(encodeArgs, "-c:v", "libx265");
        AssertContainsPair(encodeArgs, "-preset", "slow");
        AssertContainsPair(encodeArgs, "-tune", "grain");
        AssertContainsPair(encodeArgs, "-g", "150");
        AssertContainsPair(encodeArgs, "-crf", "30");
        Assert.DoesNotContain("libx264", encodeArgs);
        Assert.DoesNotContain("veryfast", encodeArgs);
    }

    [Fact]
    public async Task OptimizeCrfAsync_ReturnsStartCrf_WhenDurationProbeFails()
    {
        var processManager = new RecordingProcessManager();
        var segmentCalculator = new FixedSegmentCalculator([0]);
        var mediaProber = new FixedMediaProber { ThrowOnProbe = true };
        var parser = new DelegatingVmafParser(_ => 100);
        var service = CreateService(processManager, segmentCalculator, mediaProber, parser);

        var result = await service.OptimizeCrfAsync(CreateConversion(), requiredVmaf: 90, startCrf: 28, CancellationToken.None);

        Assert.Equal(28, result);
        Assert.Empty(processManager.Requests);
    }

    [Fact]
    public async Task OptimizeCrfAsync_ReturnsStartCrf_WhenEncodingAlwaysFails()
    {
        var processManager = new RecordingProcessManager { FailEncode = true };
        var segmentCalculator = new FixedSegmentCalculator([0]);
        var mediaProber = new FixedMediaProber();
        var parser = new DelegatingVmafParser(_ => 100);
        var service = CreateService(processManager, segmentCalculator, mediaProber, parser);

        var result = await service.OptimizeCrfAsync(CreateConversion(), requiredVmaf: 95, startCrf: 40, CancellationToken.None);

        Assert.Equal(40, result);
    }

    [Fact]
    public async Task OptimizeCrfAsync_StopsWithinBoundedIterations_WhenTargetNeverReached()
    {
        var processManager = new RecordingProcessManager();
        var segmentCalculator = new FixedSegmentCalculator([0]);
        var mediaProber = new FixedMediaProber();
        var parser = new DelegatingVmafParser(_ => 10);
        var service = CreateService(processManager, segmentCalculator, mediaProber, parser);

        var result = await service.OptimizeCrfAsync(CreateConversion(), requiredVmaf: 99, startCrf: 63, CancellationToken.None);

        Assert.Equal(63, result);
        Assert.InRange(processManager.EncodeRequestCount, 1, 20);
    }

    [Fact]
    public async Task OptimizeCrfAsync_ReturnsStartCrf_WhenVmafProcessFails()
    {
        var processManager = new RecordingProcessManager { FailVmaf = true };
        var segmentCalculator = new FixedSegmentCalculator([0]);
        var mediaProber = new FixedMediaProber();
        var parser = new DelegatingVmafParser(_ => 100);
        var service = CreateService(processManager, segmentCalculator, mediaProber, parser);

        var result = await service.OptimizeCrfAsync(CreateConversion(), requiredVmaf: 95, startCrf: 32, CancellationToken.None);

        Assert.Equal(32, result);
    }

    private static CrfOptimizationService CreateService(
        IProcessManager processManager,
        IVmafSegmentCalculator segmentCalculator,
        IMediaProber mediaProber,
        IVmafOutputParser outputParser,
        FfmpegOptions? ffmpegOptions = null)
    {
        var options = ffmpegOptions ?? new FfmpegOptions();
        var vmafOptions = new VmafOptions
        {
            SegmentCount = 1,
            SegmentDurationSeconds = 10,
            TimeoutSeconds = 30,
        };

        return new CrfOptimizationService(
            processManager,
            segmentCalculator,
            mediaProber,
            Options.Create(options),
            Options.Create(vmafOptions),
            outputParser,
            NullLogger<CrfOptimizationService>.Instance);
    }

    private static ConversionEntity CreateConversion()
    {
        return new ConversionEntity
        {
            Id = 1,
            InputPath = "input.mp4",
            OutputDirectory = ".",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Resolution = 1080,
            DetectedCrop = null,
        };
    }

    private static void AssertContainsPair(IReadOnlyList<string> args, string key, string value)
    {
        var index = args
            .Select((entry, i) => (entry, i))
            .First(tuple => string.Equals(tuple.entry, key, StringComparison.Ordinal))
            .i;
        Assert.True(index + 1 < args.Count, $"Expected value after {key}");
        Assert.Equal(value, args[index + 1]);
    }

    private sealed class FixedSegmentCalculator(IReadOnlyList<double> segments) : IVmafSegmentCalculator
    {
        public IReadOnlyList<double> CalculateSegmentStartTimes(
            double videoDurationSeconds,
            int segmentCount,
            int segmentDurationSeconds)
        {
            return segments;
        }
    }

    private sealed class FixedMediaProber : IMediaProber
    {
        public bool ThrowOnProbe { get; set; }

        public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
        {
            if (ThrowOnProbe)
            {
                throw new InvalidOperationException("Probe failed");
            }

            return Task.FromResult(new MediaProbeResult
            {
                Streams = [],
                DurationSeconds = 120,
                VideoWidth = 1920,
                VideoHeight = 1080,
            });
        }
    }

    private sealed class DelegatingVmafParser(Func<string, double?> parser) : IVmafOutputParser
    {
        public double? Parse(string ffmpegOutput)
        {
            return parser(ffmpegOutput);
        }
    }

    private sealed class RecordingProcessManager : IProcessManager
    {
        public List<ProcessRequest> Requests { get; } = [];

        public bool FailEncode { get; set; }

        public bool FailVmaf { get; set; }

        public int EncodeRequestCount => Requests.Count(r => !r.Arguments.Contains("-lavfi", StringComparer.Ordinal));

        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            var isVmaf = request.Arguments.Contains("-lavfi", StringComparer.Ordinal);
            if (!isVmaf)
            {
                if (FailEncode)
                {
                    return Task.FromResult(new ProcessResult
                    {
                        IsSuccess = false,
                        ExitCode = 1,
                        ErrorMessage = "encode-failed",
                    });
                }

                return Task.FromResult(new ProcessResult
                {
                    IsSuccess = true,
                    ExitCode = 0,
                });
            }

            if (FailVmaf)
            {
                return Task.FromResult(new ProcessResult
                {
                    IsSuccess = false,
                    ExitCode = 1,
                    ErrorMessage = "vmaf-failed",
                });
            }

            var lavfiIndex = request.Arguments
                .Select((value, index) => (value, index))
                .First(tuple => string.Equals(tuple.value, "-lavfi", StringComparison.Ordinal))
                .index;
            var filter = request.Arguments[lavfiIndex + 1];
            var logFileName = ExtractLogPathFromFilter(filter);
            if (!string.IsNullOrWhiteSpace(logFileName))
            {
                var directory = request.WorkingDirectory ?? Path.GetTempPath();
                _ = Directory.CreateDirectory(directory);
                var fullPath = Path.Combine(directory, logFileName);
                File.WriteAllText(fullPath, "{\"vmaf\":95}");
            }

            return Task.FromResult(new ProcessResult
            {
                IsSuccess = true,
                ExitCode = 0,
                StandardOutput = "ok",
            });
        }

        private static string? ExtractLogPathFromFilter(string filter)
        {
            const string marker = "log_path=";
            var markerIndex = filter.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            var start = markerIndex + marker.Length;
            var end = filter.IndexOf(':', start);
            if (end < 0)
            {
                end = filter.Length;
            }

            return filter[start..end];
        }
    }
}
