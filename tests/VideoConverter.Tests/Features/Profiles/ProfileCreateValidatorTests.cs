using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Web.Features.Profiles.Dtos;
using VideoConverter.Web.Features.Profiles.Validation;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Tests.Features.Profiles;

public class ProfileCreateValidatorTests
{
    private readonly ProfileCreateValidator _validator = new(NullLogger<ProfileCreateValidator>.Instance, new FileSystemValidator());

    [Fact]
    public async Task ValidateAsync_WithValidValues_ReturnsValid()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-prof-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);
        var dto = new ProfileCreateDto(tempDir, 20, 192, null, 1080, null, null, null);
        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        try
        {
            _ = result.IsValid.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidValues_ReturnsErrors()
    {
        var dto = new ProfileCreateDto("C:/invalid<>dir", 100, 0, (Web.Shared.Domain.ValueObjects.AudioChannels)99, 1440, "|invalid|", null, null);
        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
    }
}


