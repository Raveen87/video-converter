using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;
using VideoConverter.Common.Validation;
using VideoConverter.Common.Validation.Requests;

namespace VideoConverter.Tests.Features.Profiles;

public class ProfileCreateValidatorTests
{
    private readonly ProfileCreateValidator _validator = new(NullLogger<ProfileCreateValidator>.Instance, new FileSystemValidator(), new RequestPropertyValidator());

    [Fact]
    public async Task ValidateAsync_WithValidValues_ReturnsValid()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-prof-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);
        var dto = new ProfileCreateDto(
            "Test Profile",
            tempDir,
            20,
            192,
            AudioChannels: null,
            1080,
            AudioLanguages: null,
            SubtitleLanguages: null);
        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
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
        var dto = new ProfileCreateDto("Test Profile", "C:/invalid<>dir", 100, 0, (AudioChannels)99, 1440, AudioLanguages: null, SubtitleLanguages: null, RequiredVmaf: 101);
        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
        _ = result.ToDictionary().Should().ContainKey(nameof(dto.RequiredVmaf));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(100.0)]
    public async Task ValidateAsync_WithRequiredVmafBoundaryValues_ReturnsValid(double requiredVmaf)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-prof-vmaf-boundary-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        var dto = new ProfileCreateDto(
            "Test Profile",
            tempDir,
            20,
            192,
            AudioChannels: null,
            1080,
            AudioLanguages: null,
            SubtitleLanguages: null,
            VmafEnabled: true,
            RequiredVmaf: requiredVmaf);

        var result = await _validator.ValidateAsync(dto, CancellationToken.None);

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
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-prof-vmaf-range-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        var dto = new ProfileCreateDto(
            "Test Profile",
            tempDir,
            20,
            192,
            AudioChannels: null,
            1080,
            AudioLanguages: null,
            SubtitleLanguages: null,
            RequiredVmaf: requiredVmaf);

        var result = await _validator.ValidateAsync(dto, CancellationToken.None);

        try
        {
            _ = result.IsValid.Should().BeFalse();
            _ = result.ToDictionary().Should().ContainKey(nameof(dto.RequiredVmaf));
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
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-prof-vmaf-disabled-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        var dto = new ProfileCreateDto(
            "Test Profile",
            tempDir,
            20,
            192,
            AudioChannels: null,
            1080,
            AudioLanguages: null,
            SubtitleLanguages: null,
            VmafEnabled: false,
            RequiredVmaf: 92.0);

        var result = await _validator.ValidateAsync(dto, CancellationToken.None);

        try
        {
            _ = result.IsValid.Should().BeFalse();
            _ = result.ToDictionary().Should().ContainKey(nameof(dto.RequiredVmaf));
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
