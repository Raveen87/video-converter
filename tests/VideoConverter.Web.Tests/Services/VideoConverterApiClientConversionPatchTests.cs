using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using VideoConverter.Common.Models;
using VideoConverter.Web.Services;

namespace VideoConverter.Web.Tests.Services;

public sealed class VideoConverterApiClientConversionPatchTests
{
    [Fact]
    public async Task UpdateConversionAsync_WithNullRequiredVmaf_EmitsExplicitNullPatchOperation()
    {
        var conversionId = 42;
        var patch = new ConversionPatchDto
        {
            VmafEnabled = true,
            RequiredVmaf = null,
        };

        var expectedResponse = CreateExpectedResponse(conversionId);

        var handler = new Mock<HttpMessageHandler>();
        string? capturedRequestBody = null;

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch
                    && req.RequestUri!.ToString().EndsWith($"api/conversions/{conversionId}", StringComparison.Ordinal)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                if (req.Content is not null)
                {
                    using var reader = new StreamReader(req.Content.ReadAsStream());
                    capturedRequestBody = reader.ReadToEnd();
                }
            })
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

        _ = await client.UpdateConversionAsync(conversionId, patch);

        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody.Should().Contain("\"path\":\"/RequiredVmaf\"");
        capturedRequestBody.Should().Contain("\"value\":null");
    }

    [Fact]
    public async Task UpdateConversionAsync_WithoutVmafChanges_DoesNotEmitRequiredVmafPatchOperation()
    {
        var conversionId = 43;
        var patch = new ConversionPatchDto
        {
            OutputDirectory = "updated-output",
            RequiredVmaf = null,
            VmafEnabled = null,
        };

        var expectedResponse = CreateExpectedResponse(conversionId);

        var handler = new Mock<HttpMessageHandler>();
        string? capturedRequestBody = null;

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch
                    && req.RequestUri!.ToString().EndsWith($"api/conversions/{conversionId}", StringComparison.Ordinal)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                if (req.Content is not null)
                {
                    using var reader = new StreamReader(req.Content.ReadAsStream());
                    capturedRequestBody = reader.ReadToEnd();
                }
            })
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

        _ = await client.UpdateConversionAsync(conversionId, patch);

        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody.Should().Contain("\"path\":\"/OutputDirectory\"");
        capturedRequestBody.Should().NotContain("\"path\":\"/RequiredVmaf\"");
    }

    private static ConversionDto CreateExpectedResponse(int conversionId)
    {
        return new ConversionDto(
            Id: conversionId,
            InputPath: "in.mp4",
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
            VmafEnabled: true,
            RequiredVmaf: null,
            VmafScore: null);
    }
}
