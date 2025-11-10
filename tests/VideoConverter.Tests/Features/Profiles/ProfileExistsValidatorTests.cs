using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Profiles.Validation;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Features.Profiles;

public class ProfileExistsValidatorTests
{
    [Fact]
    public async Task GetProfileForReadAsync_Found_ReturnsProfile()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var p = new Profile { OutputDirectory = "." };
        _ = db.Profiles.Add(p);
        _ = await db.SaveChangesAsync();
        var validator = new ProfileExistsValidator(NullLogger<ProfileExistsValidator>.Instance);
        var result = await validator.GetProfileForReadAsync(p.Id, db, CancellationToken.None);
        _ = result.Should().NotBeNull();
        _ = result.Id.Should().Be(p.Id);
    }

    [Fact]
    public async Task GetProfileForUpdateAsync_NotFound_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var validator = new ProfileExistsValidator(NullLogger<ProfileExistsValidator>.Instance);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() => validator.GetProfileForUpdateAsync(999, db, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.ProfileNotFound);
    }
}
