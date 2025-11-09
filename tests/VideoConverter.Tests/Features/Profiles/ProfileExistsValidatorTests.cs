using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Tests.TestUtils;
using VideoConverter.Web.Common.Exceptions;
using VideoConverter.Web.Features.Profiles.Validation;
using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Infrastructure.Persistence;

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
