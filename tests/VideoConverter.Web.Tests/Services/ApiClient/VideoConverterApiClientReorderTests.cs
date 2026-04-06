using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using VideoConverter.Common.Models;
using VideoConverter.Web.Services.ApiClient;
using Xunit;

namespace VideoConverter.Web.Tests.Services;

public sealed class VideoConverterApiClientReorderTests
{
    [Fact]
    public async Task ReorderQueueAsync_WithValidIds_CallsCorrectEndpoint()
    {
        var orderedIds = new[] { 1, 2, 3 };
        var expectedResponse = new[]
        {
            CreateConversionDto(1, "a.mp4"),
            CreateConversionDto(2, "b.mp4"),
            CreateConversionDto(3, "c.mp4"),
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.ToString().EndsWith("api/conversions/queue/reorder", StringComparison.Ordinal)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResponse),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        var result = await client.ReorderQueueAsync(orderedIds);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Select(c => c.Id).Should().ContainInOrder(1, 2, 3);
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri!.ToString().EndsWith("api/conversions/queue/reorder", StringComparison.Ordinal)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ReorderQueueAsync_WithHttpError_ThrowsException()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad Request"),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        var act = () => client.ReorderQueueAsync(new[] { 1, 2 });

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ReorderQueueAsync_WithEmptyList_CallsEndpoint()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(Array.Empty<ConversionDto>()),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        var result = await client.ReorderQueueAsync(Array.Empty<int>());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    private static ConversionDto CreateConversionDto(int id, string inputPath)
    {
        return new ConversionDto(
            Id: id,
            InputPath: inputPath,
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
            OutputPathTemplate: null);
    }
}
