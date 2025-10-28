using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Web.Features.Profiles.Dtos;
using VideoConverter.Web.Features.Profiles.Validation;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Tests.Features.Profiles;

public class ProfilePatchValidatorTests
{
    private readonly ProfilePatchValidator _validator = new(NullLogger<ProfilePatchValidator>.Instance, new FileSystemValidator(), new RequestPropertyValidator());

    [Fact]
    public async Task ValidateAsync_WithNoFields_ReturnsValid()
    {
        var dto = new ProfilePatchDto();
        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidValues_ReturnsErrors()
    {
        var dto = new ProfilePatchDto { OutputDirectory = "C:/invalid<>dir", Crf = 100, AudioBitrateKbps = 0, Resolution = 1440, AudioChannels = (Web.Shared.Domain.ValueObjects.AudioChannels)99, MetadataBasedFilename = "|invalid|" };
        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
    }
}


