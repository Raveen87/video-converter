using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;
using VideoConverter.Common.Validation;
using VideoConverter.Common.Validation.Requests;

namespace VideoConverter.Tests.Features.Conversions;

public class ConversionPatchValidatorTests
{
    private readonly ConversionPatchValidator _validator = new(NullLogger<ConversionPatchValidator>.Instance, new FileSystemValidator(), new RequestPropertyValidator());

    [Fact]
    public async Task ValidateAsync_WithNoFields_ReturnsValid()
    {
        var dto = new ConversionPatchDto();
        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithValidValues_ReturnsValid()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-valid-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        var dto = new ConversionPatchDto
        {
            OutputDirectory = tempDir,
            Crf = 23,
            AudioBitrateKbps = 128,
            Resolution = 1080,
            AudioChannels = AudioChannels.Stereo20,
            AudioLanguages = ["English"],
            SubtitleLanguages = ["English"],
        };

        try
        {
            var result = await _validator.ValidateAsync(dto, CancellationToken.None);
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
        var dto = new ConversionPatchDto
        {
            OutputDirectory = "C:/invalid<>dir",
            Crf = 100,
            AudioBitrateKbps = 0,
            Resolution = 1440,
            AudioChannels = (AudioChannels)99,
        };
        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithPartialFields_ValidatesOnlyProvidedFields()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-valid-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        var dto = new ConversionPatchDto
        {
            OutputDirectory = tempDir,
            Crf = 23,

            // Other fields not set
        };

        try
        {
            var result = await _validator.ValidateAsync(dto, CancellationToken.None);
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
    [InlineData(0.0)]
    [InlineData(100.0)]
    public async Task ValidateAsync_WithRequiredVmafBoundaryValues_ReturnsValid(double requiredVmaf)
    {
        var dto = new ConversionPatchDto
        {
            RequiredVmaf = requiredVmaf,
            VmafEnabled = true,
        };

        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public async Task ValidateAsync_WithRequiredVmafOutOfRange_ReturnsError(double requiredVmaf)
    {
        var dto = new ConversionPatchDto
        {
            RequiredVmaf = requiredVmaf,
        };

        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
        _ = result.ToDictionary().Should().ContainKey(nameof(dto.RequiredVmaf));
    }

    [Fact]
    public async Task ValidateAsync_WithVmafDisabledAndRequiredVmafSet_ReturnsError()
    {
        var dto = new ConversionPatchDto
        {
            VmafEnabled = false,
            RequiredVmaf = 92.0,
        };

        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
        _ = result.ToDictionary().Should().ContainKey(nameof(dto.RequiredVmaf));
    }
}
