using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        var validator = new ProfileExistsValidator(NullLogger<ProfileExistsValidator>.Instance);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() => validator.GetProfileForUpdateAsync(999, db, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.ProfileNotFound);
    }
}
