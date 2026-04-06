using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;
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

namespace VideoConverter.Tests.Api;

public sealed class CharacterizationParityApiTests
{
    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Conversions_Create_List_Delete_Flow_HasStableStatusAndStateTransitions()
    {
        using var tempRoot = new TempDir();
        var outputDir = tempRoot.CreateSubdir("out");
        var inputPath = Path.Combine(tempRoot.Path, "movie.mkv");
        await File.WriteAllTextAsync(inputPath, "x", CancellationToken.None);

        await using var db = DbContextTestFactory.CreateInMemory();
        var api = CreateConversionsApi(db);

        var createRequest = new ConversionCreateDto(
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

        var createResult = await api.CreateConversionAsync(createRequest, CancellationToken.None);
        var created = createResult.Should().BeOfType<Created<ConversionDto>>().Subject;
        _ = created.StatusCode.Should().Be(StatusCodes.Status201Created);
        _ = created.Location.Should().StartWith("/api/conversions/");
        _ = created.Value.Should().NotBeNull();
        _ = created.Value!.Status.Should().Be(ConversionStatus.Pending);

        var listResult = await api.ListConversionsAsync(CancellationToken.None);
        var list = listResult.Should().BeOfType<Ok<List<ConversionDto>>>().Subject;
        _ = list.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = list.Value.Should().NotBeNull();
        _ = list.Value!.Should().ContainSingle(c => c.Id == created.Value.Id && c.Status == ConversionStatus.Pending);

        var deleteResult = await api.DeleteConversionAsync(created.Value.Id, CancellationToken.None);
        var deleted = deleteResult.Should().BeOfType<Ok<ConversionDto>>().Subject;
        _ = deleted.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = deleted.Value.Should().NotBeNull();
        _ = deleted.Value!.Status.Should().Be(ConversionStatus.Deleted);
        _ = deleted.Value.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Conversions_Delete_WhenNotFound_ThrowsNotFoundEnvelopeContract()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var api = CreateConversionsApi(db);

        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => api.DeleteConversionAsync(424242, CancellationToken.None));

        var envelope = ex.ToDictionary();
        _ = ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _ = ex.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);
        _ = envelope.Should().ContainKey("statusCode").WhoseValue.Should().Be((int)HttpStatusCode.NotFound);
        _ = envelope.Should().ContainKey("errorCode").WhoseValue.Should().Be("ConversionNotFound");
        _ = envelope.Should().ContainKey("errorMessage").WhoseValue.Should().BeOfType<string>();
        _ = envelope.Should().ContainKey("userFriendlyMessage").WhoseValue.Should().BeOfType<string>();
        _ = envelope.Should().ContainKey("timestamp");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Conversions_Update_WhenNullPatch_ThrowsValidationEnvelopeContract()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "input.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Rank = "00001000",
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var endpoints = CreateConversionsWriteApi(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => endpoints.UpdateConversionAsync(conversion.Id, null, CancellationToken.None));

        var envelope = ex.ToDictionary();
        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _ = ex.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        _ = envelope.Should().ContainKey("statusCode").WhoseValue.Should().Be((int)HttpStatusCode.BadRequest);
        _ = envelope.Should().ContainKey("errorCode").WhoseValue.Should().Be("ValidationFailed");
        _ = envelope.Should().ContainKey("errorMessage").WhoseValue.Should().BeOfType<string>();
        _ = envelope.Should().ContainKey("userFriendlyMessage").WhoseValue.Should().BeOfType<string>();
        _ = envelope.Should().ContainKey("timestamp");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Conversions_Update_WhenPatchPathInvalid_ThrowsValidationEnvelopeContract()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "input.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Rank = "00001000",
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var patch = new JsonPatchDocument<ConversionPatchDto>();
        patch.Operations.Add(new Operation<ConversionPatchDto>
        {
            op = "replace",
            path = "/doesNotExist",
            value = "value",
        });

        var endpoints = CreateConversionsWriteApi(db);
        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => endpoints.UpdateConversionAsync(conversion.Id, patch, CancellationToken.None));

        var envelope = ex.ToDictionary();
        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _ = ex.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        _ = envelope.Should().ContainKey("statusCode").WhoseValue.Should().Be((int)HttpStatusCode.BadRequest);
        _ = envelope.Should().ContainKey("errorCode").WhoseValue.Should().Be("ValidationFailed");
        _ = envelope.Should().ContainKey("errorMessage").WhoseValue.Should().BeOfType<string>();
        _ = envelope.Should().ContainKey("userFriendlyMessage").WhoseValue.Should().BeOfType<string>();
        _ = envelope.Should().ContainKey("timestamp");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Conversions_Update_WithValidPatch_HasStableSuccessStatusContract()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var conversion = new Conversion
        {
            InputPath = "input.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Rank = "00001000",
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var patch = new JsonPatchDocument<ConversionPatchDto>();
        patch.Replace(x => x.Resolution, 1080);

        var endpoints = CreateConversionsWriteApi(db);
        var result = await endpoints.UpdateConversionAsync(conversion.Id, patch, CancellationToken.None);

        var ok = result.Should().BeOfType<Ok<ConversionDto>>().Subject;
        _ = ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = ok.Value.Should().NotBeNull();
        _ = ok.Value!.Resolution.Should().Be(1080);
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Queue_Start_Pause_Resume_Stop_Endpoints_HaveStableStatusContracts()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var running = new Conversion
        {
            InputPath = "running.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
            Rank = "00001000",
        };

        var pending = new Conversion
        {
            InputPath = "pending.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Rank = "00002000",
        };

        db.Conversions.AddRange(running, pending);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);

        var startEx = await Assert.ThrowsAsync<ApiErrorException>(
            () => queueApi.StartQueueAsync(CancellationToken.None));
        _ = startEx.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _ = startEx.ErrorCode.Should().Be(ErrorCode.ConversionAlreadyStarted);
        var startEnvelope = startEx.ToDictionary();
        _ = startEnvelope.Should().ContainKey("statusCode").WhoseValue.Should().Be((int)HttpStatusCode.BadRequest);
        _ = startEnvelope.Should().ContainKey("errorCode").WhoseValue.Should().Be("ConversionAlreadyStarted");
        _ = startEnvelope.Should().ContainKey("errorMessage");
        _ = startEnvelope.Should().ContainKey("userFriendlyMessage");
        _ = startEnvelope.Should().ContainKey("timestamp");

        var pauseEx = await Assert.ThrowsAsync<ApiErrorException>(
            () => queueApi.PauseQueueAsync(CancellationToken.None));
        _ = pauseEx.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _ = pauseEx.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);

        var resumeEx = await Assert.ThrowsAsync<ApiErrorException>(
            () => queueApi.ResumeQueueAsync(CancellationToken.None));
        _ = resumeEx.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _ = resumeEx.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);

        var stopEx = await Assert.ThrowsAsync<ApiErrorException>(
            () => queueApi.StopQueueAsync(CancellationToken.None));
        _ = stopEx.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _ = stopEx.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Queue_Start_WhenPendingAndNoRunning_HasStableSuccessPayloadContract()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        _ = db.Conversions.Add(new Conversion
        {
            InputPath = "pending.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Rank = "00001000",
        });
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var result = await queueApi.StartQueueAsync(CancellationToken.None);
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        _ = statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        _ = valueResult.Value.Should().NotBeNull();

        var webJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            JsonSerializer.Serialize(valueResult.Value, webJsonOptions),
            webJsonOptions);
        _ = payload.Should().NotBeNull();
        _ = payload!.Should().ContainKey("message");
        _ = payload["message"].GetString().Should().Be("Started");
        _ = payload.Should().ContainKey("processId");
        _ = payload.Should().ContainKey("command");
        _ = payload.Should().ContainKey("conversionId");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Queue_Reorder_And_SingleReorder_HaveStableStatusAndValidationContracts()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var a = new Conversion
        {
            InputPath = "a.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Rank = "00001000",
        };
        var b = new Conversion
        {
            InputPath = "b.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1),
            Status = ConversionStatus.Pending,
            Rank = "00002000",
        };
        db.Conversions.AddRange(a, b);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);

        var reorderResult = await queueApi.ReorderQueueAsync(new QueueReorderRequestDto(new[] { b.Id, a.Id }), CancellationToken.None);
        var reorderOk = reorderResult.Should().BeOfType<Ok<List<ConversionDto>>>().Subject;
        _ = reorderOk.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = reorderOk.Value.Should().NotBeNull();
        _ = reorderOk.Value!.Select(x => x.Id).Should().ContainInOrder(b.Id, a.Id);

        var singleResult = await queueApi.ReorderAsync(a.Id, new ReorderRequestDto(b.Id, null), CancellationToken.None);
        var singleOk = singleResult.Should().BeOfType<Ok<ConversionDto>>().Subject;
        _ = singleOk.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = singleOk.Value.Should().NotBeNull();

        var invalidEx = await Assert.ThrowsAsync<ApiErrorException>(
            () => queueApi.ReorderAsync(a.Id, new ReorderRequestDto(null, null), CancellationToken.None));
        _ = invalidEx.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _ = invalidEx.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        var invalidEnvelope = invalidEx.ToDictionary();
        _ = invalidEnvelope.Should().ContainKey("statusCode").WhoseValue.Should().Be((int)HttpStatusCode.BadRequest);
        _ = invalidEnvelope.Should().ContainKey("errorCode").WhoseValue.Should().Be("ValidationFailed");
        _ = invalidEnvelope.Should().ContainKey("errorMessage");
        _ = invalidEnvelope.Should().ContainKey("userFriendlyMessage");
        _ = invalidEnvelope.Should().ContainKey("timestamp");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Profiles_Crud_And_ProfileQueueing_HaveStableStatusContracts()
    {
        using var tempRoot = new TempDir();
        var outputDir = tempRoot.CreateSubdir("out");
        var inputPath = Path.Combine(tempRoot.Path, "episode.mkv");
        await File.WriteAllTextAsync(inputPath, "x", CancellationToken.None);

        await using var db = DbContextTestFactory.CreateInMemory();
        var profilesRoutes = CreateProfilesRoutes(db);
        var profileConversions = CreateProfileConversionsApi(db);

        var createdProfile = await CreateProfileAsync(profilesRoutes, outputDir);
        var profileId = createdProfile.Value!.Id;

        var listProfiles = await profilesRoutes.GetAllProfilesAsync(CancellationToken.None);
        var profilesOk = listProfiles.Should().BeOfType<Ok<List<ProfileDto>>>().Subject;
        _ = profilesOk.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = profilesOk.Value.Should().ContainSingle(p => p.Id == profileId);

        var patch = new JsonPatchDocument<ProfilePatchDto>();
        patch.Replace(x => x.Crf, 25);
        var updatedProfile = await profilesRoutes.UpdateProfileAsync(profileId, patch, CancellationToken.None);
        var updatedOk = updatedProfile.Should().BeOfType<Ok<ProfileDto>>().Subject;
        _ = updatedOk.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = updatedOk.Value.Should().NotBeNull();
        _ = updatedOk.Value!.Crf.Should().Be(25);

        await AssertQueueConversionWithProfileAsync(profileConversions, profileId, inputPath, outputDir);
        await AssertQueueDirectoryWithProfileAsync(profileConversions, profileId, tempRoot.Path, outputDir);

        var deleteProfile = await profilesRoutes.DeleteProfileAsync(profileId, CancellationToken.None);
        var noContent = deleteProfile.Should().BeOfType<NoContent>().Subject;
        _ = noContent.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Profiles_Update_NullPatch_ThrowsValidationEnvelopeContract()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        _ = db.Profiles.Add(new Profile
        {
            Name = "P1",
            OutputDirectory = "out",
        });
        _ = await db.SaveChangesAsync();

        var endpoints = CreateProfilesRoutes(db);
        var profileId = await db.Profiles.Select(x => x.Id).FirstAsync();

        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => endpoints.UpdateProfileAsync(profileId, null, CancellationToken.None));

        var envelope = ex.ToDictionary();
        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _ = ex.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        _ = envelope.Should().ContainKey("statusCode").WhoseValue.Should().Be((int)HttpStatusCode.BadRequest);
        _ = envelope.Should().ContainKey("errorCode").WhoseValue.Should().Be("ValidationFailed");
        _ = envelope.Should().ContainKey("errorMessage");
        _ = envelope.Should().ContainKey("userFriendlyMessage");
        _ = envelope.Should().ContainKey("timestamp");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Profiles_Update_WhenPatchPathInvalid_ThrowsValidationEnvelopeContract()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        _ = db.Profiles.Add(new Profile
        {
            Name = "P1",
            OutputDirectory = "out",
        });
        _ = await db.SaveChangesAsync();

        var endpoints = CreateProfilesRoutes(db);
        var profileId = await db.Profiles.Select(x => x.Id).FirstAsync();

        var patch = new JsonPatchDocument<ProfilePatchDto>();
        patch.Operations.Add(new Operation<ProfilePatchDto>
        {
            op = "replace",
            path = "/doesNotExist",
            value = "value",
        });

        var ex = await Assert.ThrowsAsync<ApiErrorException>(
            () => endpoints.UpdateProfileAsync(profileId, patch, CancellationToken.None));

        var envelope = ex.ToDictionary();
        _ = ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _ = ex.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        _ = envelope.Should().ContainKey("statusCode").WhoseValue.Should().Be((int)HttpStatusCode.BadRequest);
        _ = envelope.Should().ContainKey("errorCode").WhoseValue.Should().Be("ValidationFailed");
        _ = envelope.Should().ContainKey("errorMessage");
        _ = envelope.Should().ContainKey("userFriendlyMessage");
        _ = envelope.Should().ContainKey("timestamp");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task Profiles_Update_WithValidPatch_HasStableSuccessStatusContract()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        _ = db.Profiles.Add(new Profile
        {
            Name = "P1",
            OutputDirectory = "out",
        });
        _ = await db.SaveChangesAsync();

        var endpoints = CreateProfilesRoutes(db);
        var profileId = await db.Profiles.Select(x => x.Id).FirstAsync();

        var patch = new JsonPatchDocument<ProfilePatchDto>();
        patch.Replace(x => x.Name, "P2");

        var result = await endpoints.UpdateProfileAsync(profileId, patch, CancellationToken.None);
        var ok = result.Should().BeOfType<Ok<ProfileDto>>().Subject;
        _ = ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = ok.Value.Should().NotBeNull();
        _ = ok.Value!.Name.Should().Be("P2");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task ManualResolution_ResolveBatch_ErrorPath_KeepsStatusAndPayloadShape()
    {
        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [],
            TmdbId = 1,
            MediaType = MediaType.Movie,
        };

        var result = await InvokeResolveBatchMetadataQueueItemsAsync(request, new Mock<IManualResolutionService>().Object, CancellationToken.None);
        var badRequest = result.Should().BeOfType<BadRequest<string>>().Subject;
        _ = badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _ = badRequest.Value.Should().Be("At least one queue item ID is required.");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task ManualResolution_ResolveBatch_SuccessPath_KeepsStatusAndPayloadShape()
    {
        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [7],
            TmdbId = 1,
            MediaType = MediaType.Movie,
        };

        var service = new Mock<IManualResolutionService>(MockBehavior.Strict);
        _ = service
            .Setup(s => s.ResolveBatchMetadataQueueItemsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchResolutionResultDto
            {
                SuccessfulIds = [7],
                Errors = [],
            });

        var result = await InvokeResolveBatchMetadataQueueItemsAsync(request, service.Object, CancellationToken.None);
        var ok = result.Should().BeOfType<Ok<BatchResolutionResultDto>>().Subject;
        _ = ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = ok.Value.Should().NotBeNull();
        _ = ok.Value!.SuccessfulIds.Should().ContainSingle().Which.Should().Be(7);
        _ = ok.Value.Errors.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task ManualResolution_ResolveStreamSelection_MissingRequiredFields_KeepsBadRequestContract()
    {
        var request = new ResolveStreamSelectionRequestDto
        {
            AudioStreamIndices = null,
            SubtitleStreamIndices = null,
            ExternalSubtitlePaths = null,
        };

        var result = await InvokeResolveStreamSelectionQueueItemAsync(1, request, new Mock<IManualResolutionService>().Object, CancellationToken.None);
        var badRequest = result.Should().BeOfType<BadRequest<string>>().Subject;
        _ = badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _ = badRequest.Value.Should().Be("At least one stream selection (audio, subtitle, or external subtitle) is required.");
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task ManualResolution_ResolveStreamSelection_SuccessPath_KeepsOkContract()
    {
        var request = new ResolveStreamSelectionRequestDto
        {
            AudioStreamIndices = "0",
            SubtitleStreamIndices = null,
            ExternalSubtitlePaths = null,
        };

        var service = new Mock<IManualResolutionService>(MockBehavior.Strict);
        _ = service
            .Setup(s => s.ResolveStreamSelectionQueueItemAsync(11, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await InvokeResolveStreamSelectionQueueItemAsync(11, request, service.Object, CancellationToken.None);
        var ok = result.Should().BeOfType<Ok>().Subject;
        _ = ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        service.VerifyAll();
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public void Parity_GlobalExceptionMapper_EmitsExpectedEnvelopeKeys()
    {
        var mapper = new ApiErrorMapper();
        var apiError = (ApiErrorException)mapper.Map(
            ApiErrorException.BusinessError(
                ErrorCode.InvalidConversionState,
                "state invalid",
                "Cannot run in current state."));

        var response = mapper.ToResponse(apiError);

        _ = mapper.GetStatusCode(apiError).Should().Be(StatusCodes.Status400BadRequest);
        _ = response.Should().ContainKey("statusCode").WhoseValue.Should().Be(StatusCodes.Status400BadRequest);
        _ = response.Should().ContainKey("errorCode").WhoseValue.Should().Be("InvalidConversionState");
        _ = response.Should().ContainKey("errorMessage").WhoseValue.Should().Be("state invalid");
        _ = response.Should().ContainKey("userFriendlyMessage").WhoseValue.Should().Be("Cannot run in current state.");
        _ = response.Should().ContainKey("timestamp");
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

    private static ConversionsWriteApi CreateConversionsWriteApi(AppDbContext db)
    {
        var patchValidator = new ConversionPatchValidator(
            NullLogger<ConversionPatchValidator>.Instance,
            new FileSystemValidator(),
            new RequestPropertyValidator());
        var existsValidator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        return new ConversionsWriteApi(db, NullLogger<ConversionsWriteApi>.Instance, patchValidator, existsValidator, new StubConversionService());
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

    private static ProfileConversionsApi CreateProfileConversionsApi(AppDbContext db)
    {
        var profileExistsValidator = new ProfileExistsValidator(NullLogger<ProfileExistsValidator>.Instance);
        var requestPropertyValidator = new RequestPropertyValidator();
        var fileSystemValidator = new FileSystemValidator();
        var conversionValidator = new ConversionCreateValidator(
            NullLogger<ConversionCreateValidator>.Instance,
            fileSystemValidator,
            requestPropertyValidator);

        return new ProfileConversionsApi(
            db,
            profileExistsValidator,
            new EffectiveConversionRequestBuilder(NullLogger<EffectiveConversionRequestBuilder>.Instance),
            conversionValidator,
            new StubConversionService(),
            new CharacterizationDirectoryBatchService(),
            new FakeNotificationService(),
            NullLogger<ProfileConversionsApi>.Instance);
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
        var task = (Task<IResult>)methodInfo!.Invoke(null, new object[] { request, service, cancellationToken })!;
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
        var task = (Task<IResult>)methodInfo!.Invoke(null, new object[] { id, request, service, cancellationToken })!;
        return await task.ConfigureAwait(false);
    }

    private static async Task<Created<ProfileDto>> CreateProfileAsync(ProfilesApiRoutes profilesRoutes, string outputDir)
    {
        var createProfile = await profilesRoutes.CreateProfileAsync(
            new ProfileCreateDto(
                Name: "Default",
                OutputDirectory: outputDir,
                Crf: 23,
                AudioBitrateKbps: 128,
                AudioChannels: AudioChannels.Stereo20,
                Resolution: 720,
                OutputFilenameTemplate: null,
                OutputPathTemplate: null,
                AudioLanguages: null,
                SubtitleLanguages: null,
                VmafEnabled: true,
                RequiredVmaf: 92.0),
            CancellationToken.None).ConfigureAwait(false);

        var createdProfile = createProfile.Should().BeOfType<Created<ProfileDto>>().Subject;
        _ = createdProfile.StatusCode.Should().Be(StatusCodes.Status201Created);
        _ = createdProfile.Value.Should().NotBeNull();
        return createdProfile;
    }

    private static async Task AssertQueueConversionWithProfileAsync(
        ProfileConversionsApi profileConversions,
        int profileId,
        string inputPath,
        string outputDir)
    {
        var queueWithProfile = await profileConversions.QueueConversionAsync(
            profileId,
            new ProfileConversionCreateDto(inputPath, outputDir, VmafEnabled: true, RequiredVmaf: 93.0),
            CancellationToken.None).ConfigureAwait(false);

        var profileQueueCreated = queueWithProfile.Should().BeOfType<Created<ConversionDto>>().Subject;
        _ = profileQueueCreated.StatusCode.Should().Be(StatusCodes.Status201Created);
        _ = profileQueueCreated.Value.Should().NotBeNull();
        _ = profileQueueCreated.Value!.Status.Should().Be(ConversionStatus.Pending);
    }

    private static async Task AssertQueueDirectoryWithProfileAsync(
        ProfileConversionsApi profileConversions,
        int profileId,
        string directoryPath,
        string outputDir)
    {
        var queueDirectoryWithProfile = await profileConversions.QueueDirectoryAsync(
            profileId,
            new ProfileDirectoryQueueRequestDto(
                DirectoryPath: directoryPath,
                OutputDirectory: outputDir,
                Extensions: new[] { ".mkv" },
                Crf: null,
                AudioBitrateKbps: null,
                AudioChannels: null,
                Resolution: null,
                AudioLanguages: null,
                SubtitleLanguages: null,
                OutputFilenameTemplate: null,
                OutputPathTemplate: null,
                VmafEnabled: null,
                RequiredVmaf: null),
            CancellationToken.None).ConfigureAwait(false);

        var accepted = queueDirectoryWithProfile.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        _ = accepted.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }

    private sealed class CharacterizationDirectoryBatchService : IDirectoryBatchService
    {
        public Task<int> CreateBatchAsync(DirectoryQueueRequestDto request, int? profileId, CancellationToken ct)
        {
            return Task.FromResult(1001);
        }
    }

    private sealed class StubConversionService : IConversionService
    {
        public Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Conversion
            {
                InputPath = request.InputPath,
                OutputDirectory = request.OutputDirectory,
                CreatedAtUtc = DateTime.UtcNow,
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
