using Microsoft.AspNetCore.Http.HttpResults;
using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Conversions.Validation;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Metadata.Api;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Features.Profiles.Api;
using VideoConverter.Api.Features.Profiles.Validation;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Tests.Fakes;
using VideoConverter.Tests.TestUtils;
using VideoConverter.Tests.TestUtils.ContractSnapshots;

namespace VideoConverter.Tests.Api;

public sealed class ContractSnapshotParityApiTests
{
    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Conversions_Create_PayloadSnapshot_IsStable() => ConversionsCreatePayloadSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Conversions_List_PayloadSnapshot_IsStable() => ConversionsListPayloadSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Conversions_Update_NullPatch_ErrorEnvelopeSnapshot_IsStable() => ConversionsUpdateNullPatchErrorEnvelopeSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Conversions_Update_InvalidPatchPath_ErrorEnvelopeSnapshot_IsStable() => ConversionsUpdateInvalidPatchPathErrorEnvelopeSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Queue_Reorder_PayloadSnapshot_IsStable() => QueueReorderPayloadSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Queue_Start_ErrorEnvelopeSnapshot_IsStable() => QueueStartErrorEnvelopeSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Queue_Start_SuccessPayloadSnapshot_IsStable() => QueueStartSuccessPayloadSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Queue_ReorderSingle_ValidationErrorEnvelopeSnapshot_IsStable() => QueueReorderSingleValidationErrorEnvelopeSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Profiles_Update_NullPatch_ErrorEnvelopeSnapshot_IsStable() => ProfilesUpdateNullPatchErrorEnvelopeSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task Profiles_Update_InvalidPatchPath_ErrorEnvelopeSnapshot_IsStable() => ProfilesUpdateInvalidPatchPathErrorEnvelopeSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task ManualResolution_ResolveBatch_InvalidRequestPayloadSnapshot_IsStable() => ManualResolutionResolveBatchInvalidRequestPayloadSnapshotIsStableCoreAsync();

    [Fact]
    [Trait("Category", "ContractSnapshots")]
    [Trait("Category", "Parity")]
    public Task ManualResolution_ResolveStreams_InvalidRequestPayloadSnapshot_IsStable() => ManualResolutionResolveStreamsInvalidRequestPayloadSnapshotIsStableCoreAsync();

    private static async Task ConversionsCreatePayloadSnapshotIsStableCoreAsync()
    {
        using var tempRoot = new TempDir();
        var outputDir = tempRoot.CreateSubdir("out");
        var inputPath = Path.Combine(tempRoot.Path, "movie.mkv");
        await File.WriteAllTextAsync(inputPath, "x", CancellationToken.None).ConfigureAwait(false);

        using var db = DbContextTestFactory.CreateInMemory();
        var api = CreateConversionsApi(db);
        var request = new ConversionCreateDto(
            InputPath: inputPath,
            OutputDirectory: outputDir,
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 720,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            VmafEnabled: null,
            RequiredVmaf: null);

        var result = await api.CreateConversionAsync(request, CancellationToken.None).ConfigureAwait(false);
        var created = result.Should().BeOfType<Created<ConversionDto>>().Subject;
        _ = created.StatusCode.Should().Be(StatusCodes.Status201Created);
        _ = created.Value.Should().NotBeNull();

        var snapshotShape = ToSnapshotShape(created.Value!, inputPathOverride: "movie.mkv", outputDirectoryOverride: "out");
        ContractSnapshotAssert.PayloadMatchesSnapshot("Conversions.Create.Success.json", snapshotShape);
    }

    private static async Task ConversionsListPayloadSnapshotIsStableCoreAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        db.Conversions.AddRange(
            new Conversion
            {
                Id = 301,
                InputPath = "fixtures-a.mkv",
                OutputDirectory = "fixtures-out",
                CreatedAtUtc = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc),
                Status = ConversionStatus.Pending,
                Rank = "00001000",
                DetectedGrainLevel = GrainLevel.None,
                VmafEnabled = false,
            },
            new Conversion
            {
                Id = 302,
                InputPath = "fixtures-b.mkv",
                OutputDirectory = "fixtures-out",
                CreatedAtUtc = new DateTime(2024, 01, 01, 0, 1, 0, DateTimeKind.Utc),
                Status = ConversionStatus.Pending,
                Rank = "00002000",
                DetectedGrainLevel = GrainLevel.None,
                VmafEnabled = false,
            });
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        var api = CreateConversionsApi(db);
        var result = await api.ListConversionsAsync(CancellationToken.None).ConfigureAwait(false);

        var ok = result.Should().BeOfType<Ok<List<ConversionDto>>>().Subject;
        _ = ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = ok.Value.Should().NotBeNull();

        ContractSnapshotAssert.PayloadMatchesSnapshot(
            "Conversions.List.Success.json",
            ok.Value!.Select(x => ToSnapshotShape(x)).ToList());
    }

    private static async Task ConversionsUpdateNullPatchErrorEnvelopeSnapshotIsStableCoreAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "input.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            Status = ConversionStatus.Pending,
            Rank = "00001000",
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        var endpoints = CreateConversionsWriteApi(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => endpoints.UpdateConversionAsync(conversion.Id, null, CancellationToken.None)).ConfigureAwait(false);

        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = ex.ToDictionary();
        AssertErrorEnvelopeKeys(envelope);
        ContractSnapshotAssert.PayloadMatchesSnapshot("Conversions.Update.NullPatch.Error.json", envelope);
    }

    private static async Task ConversionsUpdateInvalidPatchPathErrorEnvelopeSnapshotIsStableCoreAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "input.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            Status = ConversionStatus.Pending,
            Rank = "00001000",
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        var patch = new Microsoft.AspNetCore.JsonPatch.SystemTextJson.JsonPatchDocument<ConversionPatchDto>();
        patch.Operations.Add(new Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations.Operation<ConversionPatchDto>
        {
            op = "replace",
            path = "/doesNotExist",
            value = "x",
        });

        var endpoints = CreateConversionsWriteApi(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => endpoints.UpdateConversionAsync(conversion.Id, patch, CancellationToken.None)).ConfigureAwait(false);

        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = ex.ToDictionary();
        AssertErrorEnvelopeKeys(envelope);
        ContractSnapshotAssert.PayloadMatchesSnapshot("Conversions.Update.InvalidPatchPath.Error.json", envelope);
    }

    private static async Task QueueReorderPayloadSnapshotIsStableCoreAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        var first = new Conversion
        {
            Id = 201,
            InputPath = "queue-a.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = new DateTime(2024, 01, 01, 0, 1, 0, DateTimeKind.Utc),
            Status = ConversionStatus.Pending,
            Rank = "00001000",
            DetectedGrainLevel = GrainLevel.None,
            VmafEnabled = false,
        };
        var second = new Conversion
        {
            Id = 202,
            InputPath = "queue-b.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            Status = ConversionStatus.Pending,
            Rank = "00002000",
            DetectedGrainLevel = GrainLevel.None,
            VmafEnabled = false,
        };

        db.Conversions.AddRange(first, second);
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        var queueApi = CreateConversionsQueueApi(db);
        var result = await queueApi
            .ReorderQueueAsync(new QueueReorderRequestDto([second.Id, first.Id]), CancellationToken.None)
            .ConfigureAwait(false);

        var ok = result.Should().BeOfType<Ok<List<ConversionDto>>>().Subject;
        _ = ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = ok.Value.Should().NotBeNull();

        ContractSnapshotAssert.PayloadMatchesSnapshot(
            "Conversions.Queue.Reorder.Success.json",
            ok.Value!.Select(x => ToSnapshotShape(x)).ToList());
    }

    private static async Task QueueStartErrorEnvelopeSnapshotIsStableCoreAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        db.Conversions.AddRange(
            new Conversion
            {
                InputPath = "running.mkv",
                OutputDirectory = "out",
                CreatedAtUtc = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc),
                Status = ConversionStatus.Running,
                Rank = "00001000",
            },
            new Conversion
            {
                InputPath = "pending.mkv",
                OutputDirectory = "out",
                CreatedAtUtc = new DateTime(2024, 01, 01, 0, 1, 0, DateTimeKind.Utc),
                Status = ConversionStatus.Pending,
                Rank = "00002000",
            });
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        var queueApi = CreateConversionsQueueApi(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(() => queueApi.StartQueueAsync(CancellationToken.None)).ConfigureAwait(false);

        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = ex.ToDictionary();
        AssertErrorEnvelopeKeys(envelope);
        ContractSnapshotAssert.PayloadMatchesSnapshot("Conversions.Queue.Start.Error.json", envelope);
    }

    private static async Task QueueStartSuccessPayloadSnapshotIsStableCoreAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        _ = db.Conversions.Add(new Conversion
        {
            Id = 902,
            InputPath = "pending.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            Status = ConversionStatus.Pending,
            Rank = "00001000",
        });
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        var queueApi = CreateConversionsQueueApi(db);
        var result = await queueApi.StartQueueAsync(CancellationToken.None).ConfigureAwait(false);

        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        _ = statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        _ = valueResult.Value.Should().NotBeNull();

        ContractSnapshotAssert.PayloadMatchesSnapshot("Conversions.Queue.Start.Success.json", valueResult.Value!);
    }

    private static async Task QueueReorderSingleValidationErrorEnvelopeSnapshotIsStableCoreAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "single-reorder.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            Status = ConversionStatus.Pending,
            Rank = "00001000",
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        var queueApi = CreateConversionsQueueApi(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => queueApi.ReorderAsync(conversion.Id, new ReorderRequestDto(null, null), CancellationToken.None)).ConfigureAwait(false);

        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = ex.ToDictionary();
        AssertErrorEnvelopeKeys(envelope);
        ContractSnapshotAssert.PayloadMatchesSnapshot("Conversions.Queue.ReorderSingle.ValidationError.json", envelope);
    }

    private static async Task ProfilesUpdateNullPatchErrorEnvelopeSnapshotIsStableCoreAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        _ = db.Profiles.Add(new Profile
        {
            Name = "Default",
            OutputDirectory = "out",
        });
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        var profileId = await db.Profiles.Select(x => x.Id).FirstAsync(CancellationToken.None).ConfigureAwait(false);
        var profilesRoutes = CreateProfilesRoutes(db);

        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => profilesRoutes.UpdateProfileAsync(profileId, null, CancellationToken.None)).ConfigureAwait(false);

        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = ex.ToDictionary();
        AssertErrorEnvelopeKeys(envelope);
        ContractSnapshotAssert.PayloadMatchesSnapshot("Profiles.Update.NullPatch.Error.json", envelope);
    }

    private static async Task ProfilesUpdateInvalidPatchPathErrorEnvelopeSnapshotIsStableCoreAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        _ = db.Profiles.Add(new Profile
        {
            Name = "Default",
            OutputDirectory = "out",
        });
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        var profileId = await db.Profiles.Select(x => x.Id).FirstAsync(CancellationToken.None).ConfigureAwait(false);
        var profilesRoutes = CreateProfilesRoutes(db);
        var patch = new Microsoft.AspNetCore.JsonPatch.SystemTextJson.JsonPatchDocument<ProfilePatchDto>();
        patch.Operations.Add(new Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations.Operation<ProfilePatchDto>
        {
            op = "replace",
            path = "/doesNotExist",
            value = "x",
        });

        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => profilesRoutes.UpdateProfileAsync(profileId, patch, CancellationToken.None)).ConfigureAwait(false);

        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = ex.ToDictionary();
        AssertErrorEnvelopeKeys(envelope);
        ContractSnapshotAssert.PayloadMatchesSnapshot("Profiles.Update.InvalidPatchPath.Error.json", envelope);
    }

    private static async Task ManualResolutionResolveBatchInvalidRequestPayloadSnapshotIsStableCoreAsync()
    {
        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [],
            TmdbId = 42,
            MediaType = MediaType.Movie,
        };

        var result = await InvokeResolveBatchMetadataQueueItemsAsync(
            request,
            new Mock<IManualResolutionService>().Object,
            CancellationToken.None).ConfigureAwait(false);

        var badRequest = result.Should().BeOfType<BadRequest<string>>().Subject;
        _ = badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        ContractSnapshotAssert.PayloadMatchesSnapshot(
            "ManualResolution.ResolveBatch.InvalidRequest.Error.json",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["message"] = badRequest.Value,
            });
    }

    private static async Task ManualResolutionResolveStreamsInvalidRequestPayloadSnapshotIsStableCoreAsync()
    {
        var request = new ResolveStreamSelectionRequestDto
        {
            AudioStreamIndices = null,
            SubtitleStreamIndices = null,
            ExternalSubtitlePaths = null,
        };

        var result = await InvokeResolveStreamSelectionQueueItemAsync(
            id: 8,
            request,
            new Mock<IManualResolutionService>().Object,
            CancellationToken.None).ConfigureAwait(false);

        var badRequest = result.Should().BeOfType<BadRequest<string>>().Subject;
        _ = badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ContractSnapshotAssert.PayloadMatchesSnapshot(
            "ManualResolution.ResolveStreams.InvalidRequest.Error.json",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["message"] = badRequest.Value,
            });
    }

    private static object ToSnapshotShape(
        ConversionDto dto,
        string? inputPathOverride = null,
        string? outputDirectoryOverride = null)
    {
        return new
        {
            dto.Id,
            InputPath = inputPathOverride ?? dto.InputPath,
            OutputDirectory = outputDirectoryOverride ?? dto.OutputDirectory,
            dto.CreatedAtUtc,
            dto.Status,
            dto.Crf,
            dto.AudioBitrateKbps,
            dto.AudioChannels,
            dto.Resolution,
            dto.MetadataBasedFilename,
            dto.DeletedAtUtc,
            dto.TmdbId,
            dto.MatchedTitle,
            dto.MatchedYear,
            dto.MatchedSeason,
            dto.MatchedEpisode,
            dto.SelectedAudioStreamIndicesCsv,
            dto.SelectedSubtitleStreamIndicesCsv,
            dto.SelectedExternalSubtitlePathsCsv,
            dto.DetectedCrop,
            dto.DetectedGrainLevel,
            dto.OutputPathTemplate,
            dto.VmafEnabled,
            dto.RequiredVmaf,
            dto.VmafScore,
        };
    }

    private static void AssertErrorEnvelopeKeys(IDictionary<string, object> envelope)
    {
        _ = envelope.Should().ContainKey("statusCode");
        _ = envelope.Should().ContainKey("errorCode");
        _ = envelope.Should().ContainKey("errorMessage");
        _ = envelope.Should().ContainKey("userFriendlyMessage");
        _ = envelope.Should().ContainKey("timestamp");
    }

    private static ConversionsApi CreateConversionsApi(AppDbContext db)
    {
        var loggerFactory = NullLoggerFactory.Instance;
        var validator = new ConversionCreateValidator(
            NullLogger<ConversionCreateValidator>.Instance,
            new FileSystemValidator(),
            new RequestPropertyValidator());
        var conversionService = new StubConversionService();
        var notificationService = new FakeNotificationService();
        var progressTracker = new ProgressTracker(loggerFactory.CreateLogger<ProgressTracker>());
        var patchValidator = new ConversionPatchValidator(
            NullLogger<ConversionPatchValidator>.Instance,
            new FileSystemValidator(),
            new RequestPropertyValidator());
        var existsValidator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var endpoints = new ConversionsWriteApi(db, NullLogger<ConversionsWriteApi>.Instance, patchValidator, existsValidator, conversionService);

        return new ConversionsApi(
            db,
            validator,
            conversionService,
            notificationService,
            loggerFactory.CreateLogger<ConversionsApi>(),
            progressTracker,
            endpoints,
            new RunningProcessRegistry(),
            new ProcessController(loggerFactory.CreateLogger<ProcessController>()));
    }

    private static ConversionsQueueApi CreateConversionsQueueApi(AppDbContext db)
    {
        var loggerFactory = NullLoggerFactory.Instance;
        var executor = new ConversionExecutionService(
            db,
            new FakeConversionRunner(),
            new FakeNotificationService(),
            new FakeCrfOptimizationService(),
            NullLogger<ConversionExecutionService>.Instance);

        return new ConversionsQueueApi(
            db,
            executor,
            new RunningProcessRegistry(),
            new ProcessController(loggerFactory.CreateLogger<ProcessController>()),
            loggerFactory);
    }

    private static ConversionsWriteApi CreateConversionsWriteApi(AppDbContext db)
    {
        var patchValidator = new ConversionPatchValidator(
            NullLogger<ConversionPatchValidator>.Instance,
            new FileSystemValidator(),
            new RequestPropertyValidator());
        var existsValidator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        return new ConversionsWriteApi(db, NullLogger<ConversionsWriteApi>.Instance, patchValidator, existsValidator, new StubConversionService());
    }

    private static ProfilesApiRoutes CreateProfilesRoutes(AppDbContext db)
    {
        var requestPropertyValidator = new RequestPropertyValidator();
        var fileSystemValidator = new FileSystemValidator();
        var createValidator = new ProfileCreateValidator(
            NullLogger<ProfileCreateValidator>.Instance,
            fileSystemValidator,
            requestPropertyValidator);
        var patchValidator = new ProfilePatchValidator(
            NullLogger<ProfilePatchValidator>.Instance,
            fileSystemValidator,
            requestPropertyValidator);
        var existsValidator = new ProfileExistsValidator(NullLogger<ProfileExistsValidator>.Instance);

        return new ProfilesApiRoutes(
            db,
            NullLogger<ProfilesApiRoutes>.Instance,
            createValidator,
            patchValidator,
            existsValidator);
    }

    private static async Task<IResult> InvokeResolveBatchMetadataQueueItemsAsync(
        ResolveBatchMetadataQueueRequestDto request,
        IManualResolutionService service,
        CancellationToken cancellationToken)
    {
        var methodInfo = typeof(ManualResolutionApi).GetMethod(
            "ResolveBatchMetadataQueueItems",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        _ = methodInfo.Should().NotBeNull();
        var task = (Task<IResult>)methodInfo!.Invoke(null, [request, service, cancellationToken])!;
        return await task.ConfigureAwait(false);
    }

    private static async Task<IResult> InvokeResolveStreamSelectionQueueItemAsync(
        int id,
        ResolveStreamSelectionRequestDto request,
        IManualResolutionService service,
        CancellationToken cancellationToken)
    {
        var methodInfo = typeof(ManualResolutionApi).GetMethod(
            "ResolveStreamSelectionQueueItem",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        _ = methodInfo.Should().NotBeNull();
        var task = (Task<IResult>)methodInfo!.Invoke(null, [id, request, service, cancellationToken])!;
        return await task.ConfigureAwait(false);
    }

    private sealed class StubConversionService : IConversionService
    {
        private static readonly DateTime StableCreatedAtUtc = new(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        public Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Conversion
            {
                InputPath = request.InputPath,
                OutputDirectory = request.OutputDirectory,
                CreatedAtUtc = StableCreatedAtUtc,
                Status = ConversionStatus.Pending,
                Crf = request.Crf,
                AudioBitrateKbps = request.AudioBitrateKbps,
                AudioChannels = request.AudioChannels,
                Resolution = request.Resolution,
                RequiredVmaf = request.RequiredVmaf,
                VmafEnabled = request.VmafEnabled ?? false,
            });
        }

        public Task<Conversion> CreateConversionWithMetadataAsync(ConversionCreateDto request, MediaMetadata metadata, CancellationToken cancellationToken)
        {
            return CreateConversionAsync(request, cancellationToken);
        }

        public Task<LanguageProcessingResult> ProcessLanguagesAsync(string inputPath, IEnumerable<string>? audioLanguages, IEnumerable<string>? subtitleLanguages, CancellationToken cancellationToken)
        {
            return Task.FromResult(new LanguageProcessingResult());
        }

        public Task<Conversion> UpdateMetadataAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
