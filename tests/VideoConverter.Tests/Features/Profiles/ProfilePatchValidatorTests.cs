using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;
using VideoConverter.Common.Validation;
using VideoConverter.Common.Validation.Requests;

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
        var dto = new ProfilePatchDto { OutputDirectory = "C:/invalid<>dir", Crf = 100, AudioBitrateKbps = 0, Resolution = 1440, AudioChannels = (AudioChannels)99 };
        var result = await _validator.ValidateAsync(dto, CancellationToken.None);
        _ = result.IsValid.Should().BeFalse();
    }
}
