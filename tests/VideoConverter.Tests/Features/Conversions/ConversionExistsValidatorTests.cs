using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Validation;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Features.Conversions;

public class ConversionExistsValidatorTests
{
    [Fact]
    public async Task GetConversionForReadAsync_Found_ReturnsConversion()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var c = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(c);
        _ = await db.SaveChangesAsync();
        var validator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var result = await validator.GetConversionForReadAsync(c.Id, db, CancellationToken.None);
        _ = result.Should().NotBeNull();
        _ = result.Id.Should().Be(c.Id);
    }

    [Fact]
    public async Task GetConversionForReadAsync_NotFound_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var validator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() => validator.GetConversionForReadAsync(999, db, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);
    }

    [Fact]
    public async Task GetConversionForUpdateAsync_PendingStatus_ReturnsConversion()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var c = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(c);
        _ = await db.SaveChangesAsync();
        var validator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var result = await validator.GetConversionForUpdateAsync(c.Id, db, CancellationToken.None);
        _ = result.Should().NotBeNull();
        _ = result.Id.Should().Be(c.Id);
        _ = result.Status.Should().Be(ConversionStatus.Pending);
    }

    [Fact]
    public async Task GetConversionForUpdateAsync_RunningStatus_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var c = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
        };
        _ = db.Conversions.Add(c);
        _ = await db.SaveChangesAsync();
        var validator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() => validator.GetConversionForUpdateAsync(c.Id, db, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.InvalidConversionState);
        _ = ex.UserFriendlyMessage.Should().Contain("currently running");
    }

    [Fact]
    public async Task GetConversionForUpdateAsync_CompletedStatus_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var c = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Completed,
        };
        _ = db.Conversions.Add(c);
        _ = await db.SaveChangesAsync();
        var validator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() => validator.GetConversionForUpdateAsync(c.Id, db, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.InvalidConversionState);
    }

    [Fact]
    public async Task GetConversionForUpdateAsync_FailedStatus_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var c = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Failed,
        };
        _ = db.Conversions.Add(c);
        _ = await db.SaveChangesAsync();
        var validator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() => validator.GetConversionForUpdateAsync(c.Id, db, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.InvalidConversionState);
    }

    [Fact]
    public async Task GetConversionForUpdateAsync_NotFound_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var validator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() => validator.GetConversionForUpdateAsync(999, db, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);
    }
}