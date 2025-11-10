using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Conversions.Validation;
using VideoConverter.Api.Shared.Validation;
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
            .WithAudioChannels((VideoConverter.Api.Shared.Domain.ValueObjects.AudioChannels)99)
            .Build();

        var result = await _validator.ValidateAsync(request, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
        var dict = result.ToDictionary();
        _ = dict.Keys.Should().Contain([nameof(request.InputPath), nameof(request.OutputDirectory), nameof(request.Crf), nameof(request.AudioBitrateKbps), nameof(request.Resolution), nameof(request.AudioLanguages), nameof(request.SubtitleLanguages), nameof(request.AudioChannels)]);
    }
}
