using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Moq;
using Moq.Protected;

using VideoConverter.Common.Models;
using VideoConverter.Web.Models.ApiClient;
using VideoConverter.Web.Services.ApiClient;

namespace VideoConverter.Web.Tests.Services;

public sealed class VideoConverterApiClientContractDelegationTests
{
    [Fact]
#pragma warning disable MA0051 // Method is intentionally exhaustive contract coverage.
    public async Task PreservesPublicContractAcrossDomainFacades_WithStableRequestUrisAndPayloadShapes()
#pragma warning restore MA0051
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        var captured = new List<CapturedRequest>();

        Setup(handler, HttpMethod.Get, "api/conversions", _ => JsonResponse(Array.Empty<ConversionDto>()), captured);
        Setup(handler, HttpMethod.Post, "api/conversions", _ => JsonResponse(CreateConversionDto(1)), captured);
        Setup(handler, HttpMethod.Patch, "api/conversions/1", _ => JsonResponse(CreateConversionDto(1)), captured, "application/json-patch+json");
        Setup(handler, HttpMethod.Delete, "api/conversions/1", _ => JsonResponse(CreateConversionDto(1)), captured);
        Setup(handler, HttpMethod.Delete, "api/conversions", _ => JsonResponse(new DeletionResultDto(1, [])), captured, "application/json");
        Setup(handler, HttpMethod.Delete, "api/conversions/finished?olderThanDays=7", _ => JsonResponse(new { deleted = 1 }), captured);
        Setup(handler, HttpMethod.Post, "api/conversions/queue-directory", _ => JsonResponse(new { batchId = 11 }), captured);
        Setup(handler, HttpMethod.Get, "api/conversions/queue-directory/batches/11", _ => JsonResponse(new DirectoryBatchStatusDto(11, "Running", 1, 0, 1, null, DateTime.UtcNow, null)), captured);
        Setup(handler, HttpMethod.Get, "api/conversions/queue-directory/batches?status=running", _ => JsonResponse(Array.Empty<DirectoryBatchStatusDto>()), captured);
        Setup(handler, HttpMethod.Get, "api/conversions/queue-directory/batches/11/conversions", _ => JsonResponse(Array.Empty<BatchConversionDto>()), captured);
        Setup(handler, HttpMethod.Post, "api/conversions/queue/start", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Post, "api/conversions/queue/pause", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Post, "api/conversions/queue/resume", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Post, "api/conversions/queue/stop", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Put, "api/conversions/queue/reorder", _ => JsonResponse(Array.Empty<ConversionDto>()), captured);
        Setup(
            handler,
            HttpMethod.Get,
            "api/conversions/1/progress",
            _ => JsonResponse(new ConversionProgress
            {
                Status = ConversionProgressStatus.Running,
                PercentageComplete = 10.0,
                CurrentTimeSeconds = 1.0,
                TotalDurationSeconds = 10.0,
                EstimatedTimeRemaining = TimeSpan.FromSeconds(9),
            }),
            captured);

        Setup(handler, HttpMethod.Get, "api/profiles", _ => JsonResponse(Array.Empty<ProfileDto>()), captured);
        Setup(handler, HttpMethod.Get, "api/profiles/1", _ => JsonResponse(new ProfileDto(1, "p", null, null, null, null, null, null, null, null, null, false, null)), captured);
        Setup(handler, HttpMethod.Post, "api/profiles", _ => JsonResponse(new ProfileDto(2, "new", null, null, null, null, null, null, null, null, null, false, null)), captured);
        Setup(handler, HttpMethod.Patch, "api/profiles/1", _ => JsonResponse(new ProfileDto(1, "updated", null, null, null, null, null, null, null, null, null, false, null)), captured, "application/json-patch+json");
        Setup(handler, HttpMethod.Delete, "api/profiles/1", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Post, "api/profiles/1/conversions", _ => JsonResponse(CreateConversionDto(3)), captured);
        Setup(handler, HttpMethod.Post, "api/profiles/1/conversions/queue-directory", _ => JsonResponse(new { batchId = 12 }), captured);

        Setup(handler, HttpMethod.Get, "api/notifications", _ => JsonResponse(Array.Empty<NotificationDto>()), captured);
        Setup(handler, HttpMethod.Post, "api/notifications/5/read", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Post, "api/notifications/read-all", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Delete, "api/notifications", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Delete, "api/notifications/5", _ => EmptyOk(), captured);

        Setup(handler, HttpMethod.Get, "api/manual-resolution?type=Metadata", _ => JsonResponse(Array.Empty<ManualResolutionQueueItemDto>()), captured);
        Setup(handler, HttpMethod.Post, "api/manual-resolution/4/resolve-metadata", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Post, "api/manual-resolution/4/resolve-streams", _ => EmptyOk(), captured);
        Setup(handler, HttpMethod.Post, "api/manual-resolution/resolve-metadata-batch", _ => JsonResponse(new BatchResolutionResultDto()), captured);
        Setup(handler, HttpMethod.Delete, "api/manual-resolution/4", _ => EmptyOk(), captured);

        Setup(handler, HttpMethod.Post, "api/conversions/1/resolve-metadata", _ => JsonResponse(CreateConversionDto(1)), captured);
        Setup(handler, HttpMethod.Post, "api/conversions/resolve-metadata-batch", _ => JsonResponse(Array.Empty<ConversionDto>()), captured);
        Setup(handler, HttpMethod.Post, "api/metadata/search", _ => JsonResponse(Array.Empty<MetadataSearchResultDto>()), captured);
        Setup(handler, HttpMethod.Get, "api/tv/9/episode-groups", _ => JsonResponse(Array.Empty<EpisodeGroupDto>()), captured);
        Setup(
            handler,
            HttpMethod.Get,
            "api/tv/9/episode-groups/group-1",
            _ => JsonResponse(new EpisodeGroupDetailsDto
            {
                Id = "group-1",
                Name = "g",
                Type = "d",
                Description = null,
                Seasons = [],
            }),
            captured);

        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<VideoConverterApiClient>.Instance;
        var client = new VideoConverterApiClient(httpClient, logger);

        _ = await client.GetConversionsAsync();
        _ = await client.CreateConversionAsync(new ConversionCreateDto("in.mkv", "out", null, null, null, null, null, null));
        _ = await client.UpdateConversionAsync(1, new ConversionPatchDto { OutputDirectory = "updated", VmafEnabled = true, RequiredVmaf = null });
        _ = await client.DeleteConversionAsync(1);
        _ = await client.DeleteConversionsAsync([1]);
        _ = await client.ClearFinishedConversionsAsync(7);
        _ = await client.QueueDirectoryAsync(new DirectoryQueueRequestDto("dir", "out", null, null, null, null, null, null, null, null, null));
        _ = await client.GetBatchStatusAsync(11);
        _ = await client.GetAllBatchesAsync("running");
        _ = await client.GetBatchConversionsAsync(11);
        await client.StartQueueAsync();
        await client.PauseQueueAsync();
        await client.ResumeQueueAsync();
        await client.StopQueueAsync();
        _ = await client.ReorderQueueAsync([]);
        _ = await client.GetConversionProgressAsync(1);

        _ = await client.GetProfilesAsync();
        _ = await client.GetProfileAsync(1);
        _ = await client.CreateProfileAsync(new ProfileCreateDto("p", null, null, null, null, null, null, null, null, null, false, null));
        _ = await client.UpdateProfileAsync(1, new ProfilePatchRequest(new ProfilePatchDto { Name = "updated" }, [nameof(ProfilePatchDto.Name)]));
        await client.DeleteProfileAsync(1);
        _ = await client.QueueConversionWithProfileAsync(1, new ProfileConversionCreateDto("in.mkv", "out"));
        _ = await client.QueueDirectoryWithProfileAsync(1, new ProfileDirectoryQueueRequestDto("dir", "out", null, null, null, null, null, null, null, null, null, null, null));

        _ = await client.GetNotificationsAsync();
        await client.MarkNotificationAsReadAsync(5);
        await client.MarkAllNotificationsAsReadAsync();
        await client.ClearNotificationsAsync();
        await client.DeleteNotificationAsync(5);

        _ = await client.GetManualResolutionQueueItemsAsync(ManualResolutionType.Metadata);
        await client.ResolveMetadataQueueItemAsync(4, new ResolveMetadataRequestDto { TmdbId = 1, MediaType = MediaType.Movie });
        await client.ResolveStreamSelectionQueueItemAsync(4, new ResolveStreamSelectionRequestDto
        {
            AudioStreamIndices = null,
            SubtitleStreamIndices = null,
            ExternalSubtitlePaths = null,
        });
        _ = await client.ResolveBatchMetadataQueueItemsAsync(new ResolveBatchMetadataQueueRequestDto { QueueItemIds = [4], TmdbId = 1, MediaType = MediaType.Movie });
        await client.DeleteManualResolutionQueueItemAsync(4);

        _ = await client.ResolveConversionMetadataAsync(1, new ResolveMetadataRequestDto { TmdbId = 1, MediaType = MediaType.Movie });
        _ = await client.ResolveBatchConversionMetadataAsync(new ResolveBatchMetadataRequestDto { ConversionIds = [1], TmdbId = 1, MediaType = MediaType.Movie });
        _ = await client.SearchMetadataAsync(new MetadataSearchRequestDto { Query = "q" });
        _ = await client.GetEpisodeGroupsAsync(9);
        _ = await client.GetEpisodeGroupDetailsAsync(9, "group-1");

        captured.Select(r => r.PathAndQuery).Should().ContainInOrder(
            "api/conversions",
            "api/conversions",
            "api/conversions/1",
            "api/conversions/1",
            "api/conversions",
            "api/conversions/finished?olderThanDays=7",
            "api/conversions/queue-directory",
            "api/conversions/queue-directory/batches/11",
            "api/conversions/queue-directory/batches?status=running",
            "api/conversions/queue-directory/batches/11/conversions",
            "api/conversions/queue/start",
            "api/conversions/queue/pause",
            "api/conversions/queue/resume",
            "api/conversions/queue/stop",
            "api/conversions/queue/reorder",
            "api/conversions/1/progress",
            "api/profiles",
            "api/profiles/1",
            "api/profiles",
            "api/profiles/1",
            "api/profiles/1",
            "api/profiles/1/conversions",
            "api/profiles/1/conversions/queue-directory",
            "api/notifications",
            "api/notifications/5/read",
            "api/notifications/read-all",
            "api/notifications",
            "api/notifications/5",
            "api/manual-resolution?type=Metadata",
            "api/manual-resolution/4/resolve-metadata",
            "api/manual-resolution/4/resolve-streams",
            "api/manual-resolution/resolve-metadata-batch",
            "api/manual-resolution/4",
            "api/conversions/1/resolve-metadata",
            "api/conversions/resolve-metadata-batch",
            "api/metadata/search",
            "api/tv/9/episode-groups",
            "api/tv/9/episode-groups/group-1");

        captured.Single(r => string.Equals(r.PathAndQuery, "api/conversions/1", StringComparison.Ordinal) && string.Equals(r.Method, HttpMethod.Patch.Method, StringComparison.Ordinal))
            .ContentType.Should().Be("application/json-patch+json");
        captured.Single(r => string.Equals(r.PathAndQuery, "api/profiles/1", StringComparison.Ordinal) && string.Equals(r.Method, HttpMethod.Patch.Method, StringComparison.Ordinal))
            .ContentType.Should().Be("application/json-patch+json");
    }

    private static void Setup(
        Mock<HttpMessageHandler> handler,
        HttpMethod expectedMethod,
        string expectedPathAndQuery,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        List<CapturedRequest> captured,
        string? expectedContentType = null)
    {
        _ = handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == expectedMethod
                    && req.RequestUri != null
                    && req.RequestUri.PathAndQuery == "/" + expectedPathAndQuery
                    && (expectedContentType == null || req.Content!.Headers.ContentType!.MediaType == expectedContentType)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                captured.Add(new CapturedRequest(
                    req.Method.Method,
                    req.RequestUri!.PathAndQuery.TrimStart('/'),
                    req.Content?.Headers.ContentType?.MediaType));
            })
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) => responseFactory(req));
    }

    private static HttpResponseMessage JsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload),
        };
    }

    private static HttpResponseMessage EmptyOk()
    {
        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private static ConversionDto CreateConversionDto(int id)
    {
        return new ConversionDto(
            Id: id,
            InputPath: "in.mkv",
            OutputDirectory: "out",
            CreatedAtUtc: DateTime.UtcNow,
            Status: ConversionStatus.Pending,
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            MetadataBasedFilename: null,
            DeletedAtUtc: null,
            TmdbId: null,
            MatchedTitle: null,
            MatchedYear: null,
            MatchedSeason: null,
            MatchedEpisode: null,
            SelectedAudioStreamIndicesCsv: null,
            SelectedSubtitleStreamIndicesCsv: null,
            SelectedExternalSubtitlePathsCsv: null,
            DetectedCrop: null,
            DetectedGrainLevel: GrainLevel.Low,
            OutputPathTemplate: null,
            VmafEnabled: false,
            RequiredVmaf: null,
            VmafScore: null);
    }

    private sealed record CapturedRequest(string Method, string PathAndQuery, string? ContentType);
}
