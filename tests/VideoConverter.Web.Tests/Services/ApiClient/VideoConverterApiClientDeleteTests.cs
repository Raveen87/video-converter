using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using VideoConverter.Common.Models;
using VideoConverter.Web.Services.ApiClient;

namespace VideoConverter.Web.Tests.Services;

public sealed class VideoConverterApiClientDeleteTests
{
    [Fact]
    public async Task DeleteConversionsAsync_SendsDeleteRequestAndReturnsResult()
    {
        HttpRequestMessage? sentRequest = null;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().EndsWith("api/conversions", StringComparison.Ordinal)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => sentRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"deleted\":2,\"missingIds\":[]}", Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        var result = await client.DeleteConversionsAsync(new[] { 5, 6 });

        result.Deleted.Should().Be(2);
        result.MissingIds.Should().BeEmpty();
        sentRequest.Should().NotBeNull();

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.RequestUri!.ToString().EndsWith("api/conversions", StringComparison.Ordinal)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ClearFinishedConversionsAsync_UsesQueryParameterAndReturnsDeletedCount()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().EndsWith("api/conversions/finished?olderThanDays=30", StringComparison.Ordinal)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new { deleted = 4 }),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        var deleted = await client.ClearFinishedConversionsAsync(30);

        deleted.Should().Be(4);
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.RequestUri!.ToString().EndsWith("api/conversions/finished?olderThanDays=30", StringComparison.Ordinal)),
            ItExpr.IsAny<CancellationToken>());
    }
}
