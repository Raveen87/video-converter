using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Validation;
using VideoConverter.Web.Shared.Validation;

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

        var request = new ConversionCreateDto(tempFile, tempDir, 23, 128, AudioChannels: null, 1080, ["English"], ["English"]);
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
        var request = new ConversionCreateDto(
            InputPath: "C:/does/not/exist.mp4",
            OutputDirectory: "C:/invalid<>dir",
            Crf: 100,
            AudioBitrateKbps: 500,
            AudioChannels: (Web.Shared.Domain.ValueObjects.AudioChannels)99,
            Resolution: 1440,
            AudioLanguages: [string.Empty],
            SubtitleLanguages: ["   "]);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
        var dict = result.ToDictionary();
        _ = dict.Keys.Should().Contain([nameof(request.InputPath), nameof(request.OutputDirectory), nameof(request.Crf), nameof(request.AudioBitrateKbps), nameof(request.Resolution), nameof(request.AudioLanguages), nameof(request.SubtitleLanguages), nameof(request.AudioChannels)]);
    }
}
