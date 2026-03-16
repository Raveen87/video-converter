using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;
using VideoConverter.Common.Validation;
using VideoConverter.Common.Validation.Requests;
using VideoConverter.Tests.TestUtils.Builders;

namespace VideoConverter.Tests.Features.Conversions;

public class ConversionCreateValidatorTests
{
    private readonly ConversionCreateValidator _validator = new(NullLogger<ConversionCreateValidator>.Instance, new FileSystemValidator(), new RequestPropertyValidator());

    [Fact]
    public async Task ValidateAsync_WithValidRequest_ReturnsValid()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-valid-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "input.mp4");
        await File.WriteAllTextAsync(tempFile, "x");

        var request = new ConversionCreateDtoBuilder()
            .WithInputPath(tempFile)
            .WithOutputDirectory(tempDir)
            .WithCrf(23)
            .WithAudioBitrateKbps(128)
            .WithResolution(1080)
            .WithAudioLanguages("English")
            .WithSubtitleLanguages("English")
            .Build();
        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        try
        {
            _ = result.IsValid.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidValues_ReturnsErrors()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("C:/does/not/exist.mp4")
            .WithOutputDirectory("C:/invalid<>dir")
            .WithCrf(100)
            .WithAudioBitrateKbps(500)
            .WithResolution(1440)
            .WithAudioLanguages(string.Empty)
            .WithSubtitleLanguages("   ")
            .WithAudioChannels((AudioChannels)99)
            .WithRequiredVmaf(101)
            .Build();

        var result = await _validator.ValidateAsync(request, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
        var dict = result.ToDictionary();
        _ = dict.Keys.Should().Contain([nameof(request.InputPath), nameof(request.OutputDirectory), nameof(request.Crf), nameof(request.AudioBitrateKbps), nameof(request.Resolution), nameof(request.AudioLanguages), nameof(request.SubtitleLanguages), nameof(request.AudioChannels), nameof(request.RequiredVmaf)]);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(100.0)]
    public async Task ValidateAsync_WithRequiredVmafBoundaryValues_ReturnsValid(double requiredVmaf)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-vmaf-boundary-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "input.mp4");
        await File.WriteAllTextAsync(tempFile, "x");

        var request = new ConversionCreateDtoBuilder()
            .WithInputPath(tempFile)
            .WithOutputDirectory(tempDir)
            .WithVmafEnabled(true)
            .WithRequiredVmaf(requiredVmaf)
            .Build();

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        try
        {
            _ = result.IsValid.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public async Task ValidateAsync_WithRequiredVmafOutOfRange_ReturnsError(double requiredVmaf)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-vmaf-range-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "input.mp4");
        await File.WriteAllTextAsync(tempFile, "x");

        var request = new ConversionCreateDtoBuilder()
            .WithInputPath(tempFile)
            .WithOutputDirectory(tempDir)
            .WithRequiredVmaf(requiredVmaf)
            .Build();

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        try
        {
            _ = result.IsValid.Should().BeFalse();
            _ = result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_WithVmafDisabledAndRequiredVmafSet_ReturnsError()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-vmaf-disabled-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "input.mp4");
        await File.WriteAllTextAsync(tempFile, "x");

        var request = new ConversionCreateDtoBuilder()
            .WithInputPath(tempFile)
            .WithOutputDirectory(tempDir)
            .WithVmafEnabled(false)
            .WithRequiredVmaf(92.0)
            .Build();

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        try
        {
            _ = result.IsValid.Should().BeFalse();
            _ = result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_WithVmafUnspecifiedAndRequiredVmafSet_ReturnsError()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-vmaf-unspecified-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "input.mp4");
        await File.WriteAllTextAsync(tempFile, "x");

        var request = new ConversionCreateDtoBuilder()
            .WithInputPath(tempFile)
            .WithOutputDirectory(tempDir)
            .WithVmafEnabled(null)
            .WithRequiredVmaf(92.0)
            .Build();

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        try
        {
            _ = result.IsValid.Should().BeFalse();
            _ = result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
