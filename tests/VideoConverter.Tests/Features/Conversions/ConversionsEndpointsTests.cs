using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Conversions.Validation;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Features.Conversions;

public class ConversionsEndpointsTests
{
    [Fact]
    public async Task UpdateConversionAsync_WithValidPatch_UpdatesConversion()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Crf = 23,
            AudioBitrateKbps = 128,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<ConversionPatchDto>();
        patchDoc.Replace(x => x.OutputDirectory, "new-output");
        patchDoc.Replace(x => x.Crf, 28);

        var endpoints = CreateEndpoints(db);
        var result = await endpoints.UpdateConversionAsync(conversion.Id, patchDoc, CancellationToken.None);

        _ = result.Should().NotBeNull();
        var statusCode = ((Microsoft.AspNetCore.Http.HttpResults.Ok<ConversionDto>)result).StatusCode;
        _ = statusCode.Should().Be(200);

        var updated = await db.Conversions.FindAsync(conversion.Id);
        _ = updated.Should().NotBeNull();
        _ = updated.OutputDirectory.Should().Be("new-output");
        _ = updated.Crf.Should().Be(28);
    }

    [Fact]
    public async Task UpdateConversionAsync_WithRunningStatus_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<ConversionPatchDto>();
        patchDoc.Replace(x => x.OutputDirectory, "new-output");

        var endpoints = CreateEndpoints(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() =>
            endpoints.UpdateConversionAsync(conversion.Id, patchDoc, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.InvalidConversionState);
        _ = ex.UserFriendlyMessage.Should().Contain("currently running");
    }

    [Fact]
    public async Task UpdateConversionAsync_WithCompletedStatus_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Completed,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<ConversionPatchDto>();
        patchDoc.Replace(x => x.OutputDirectory, "new-output");

        var endpoints = CreateEndpoints(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() =>
            endpoints.UpdateConversionAsync(conversion.Id, patchDoc, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.InvalidConversionState);
    }

    [Fact]
    public async Task UpdateConversionAsync_WithFailedStatus_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Failed,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<ConversionPatchDto>();
        patchDoc.Replace(x => x.OutputDirectory, "new-output");

        var endpoints = CreateEndpoints(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() =>
            endpoints.UpdateConversionAsync(conversion.Id, patchDoc, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.InvalidConversionState);
    }

    [Fact]
    public async Task UpdateConversionAsync_WithCanceledStatus_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Canceled,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<ConversionPatchDto>();
        patchDoc.Replace(x => x.OutputDirectory, "new-output");

        var endpoints = CreateEndpoints(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() =>
            endpoints.UpdateConversionAsync(conversion.Id, patchDoc, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.InvalidConversionState);
        _ = ex.UserFriendlyMessage.Should().Contain("canceled");
    }

    [Fact]
    public async Task UpdateConversionAsync_WithNotFound_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var patchDoc = new JsonPatchDocument<ConversionPatchDto>();
        patchDoc.Replace(x => x.OutputDirectory, "new-output");

        var endpoints = CreateEndpoints(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() =>
            endpoints.UpdateConversionAsync(999, patchDoc, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);
    }

    [Fact]
    public async Task UpdateConversionAsync_WithNullPatch_ThrowsApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var endpoints = CreateEndpoints(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() =>
            endpoints.UpdateConversionAsync(conversion.Id, null, CancellationToken.None));
        _ = ex.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
    }

    [Fact]
    public async Task UpdateConversionAsync_WithPartialUpdate_UpdatesOnlySpecifiedFields()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Crf = 23,
            AudioBitrateKbps = 128,
            Resolution = 1080,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<ConversionPatchDto>();
        patchDoc.Replace(x => x.Crf, 28);

        var endpoints = CreateEndpoints(db);
        _ = await endpoints.UpdateConversionAsync(conversion.Id, patchDoc, CancellationToken.None);

        var updated = await db.Conversions.FindAsync(conversion.Id);
        _ = updated.Should().NotBeNull();
        _ = updated.Crf.Should().Be(28);
        _ = updated.OutputDirectory.Should().Be("output"); // Unchanged
        _ = updated.Resolution.Should().Be(1080); // Unchanged
    }

    private static ConversionsEndpoints CreateEndpoints(AppDbContext db)
    {
        var logger = NullLogger<ConversionsEndpoints>.Instance;
        var patchValidator = new ConversionPatchValidator(
            NullLogger<ConversionPatchValidator>.Instance,
            new FileSystemValidator(),
            new RequestPropertyValidator());
        var existsValidator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var conversionService = new StubConversionServiceForUpdate();
        return new ConversionsEndpoints(db, logger, patchValidator, existsValidator, conversionService);
    }

    private sealed class StubConversionServiceForUpdate : IConversionService
    {
        public Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
        {
            var c = new Conversion
            {
                InputPath = request.InputPath,
                OutputDirectory = request.OutputDirectory,
                CreatedAtUtc = DateTime.UtcNow,
                Crf = request.Crf,
                AudioBitrateKbps = request.AudioBitrateKbps,
                Resolution = request.Resolution,
            };
            return Task.FromResult(c);
        }

        public Task<Conversion> CreateConversionWithMetadataAsync(ConversionCreateDto request, MediaMetadata metadata, CancellationToken cancellationToken)
        {
            // For testing, update doesn't trigger new creation flow usually, but we implement for interface
            return CreateConversionAsync(request, cancellationToken);
        }

        public Task<LanguageProcessingResult> ProcessLanguagesAsync(string inputPath, IEnumerable<string>? audioLanguages, IEnumerable<string>? subtitleLanguages, CancellationToken cancellationToken)
        {
            return Task.FromResult(new LanguageProcessingResult
            {
                AudioIndicesCsv = null,
                SubtitleIndicesCsv = null,
                ExternalSubtitlePathsCsv = null,
            });
        }

        public Task<Conversion> UpdateMetadataAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Not used in this test");
        }
    }
}
