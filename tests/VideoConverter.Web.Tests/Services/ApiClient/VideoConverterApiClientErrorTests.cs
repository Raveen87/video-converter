using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using VideoConverter.Common.Models;
using VideoConverter.Web.Services.ApiClient;
using Xunit;

namespace VideoConverter.Web.Tests.Services;

public class VideoConverterApiClientErrorTests
{
    [Fact]
    public async Task CreateConversionAsync_WhenErrorWithUserFriendlyMessage_ThrowsExceptionWithMessage()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        var errorJson = "{\"userFriendlyMessage\":\"Custom error message\",\"errorMessage\":\"Technical error\"}";

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(errorJson),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);
        var request = new ConversionCreateDto("in.mkv", "out", null, null, null, null, null, null);

        // Act
        var act = () => client.CreateConversionAsync(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Custom error message*");
    }

    [Fact]
    public async Task CreateConversionAsync_WhenErrorWithValidationErrors_ThrowsExceptionWithDetails()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        var errorJson = "{\"fieldErrors\":{\"InputPath\":[\"File does not exist\"]}}";

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(errorJson),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);
        var request = new ConversionCreateDto("in.mkv", "out", null, null, null, null, null, null);

        // Act
        var act = () => client.CreateConversionAsync(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Validation errors*File does not exist*");
    }

    [Fact]
    public async Task QueueDirectoryAsync_WhenErrorWithErrorMessageOnly_ThrowsExceptionWithMessage()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        var errorJson = "{\"errorMessage\":\"Technical error message\"}";

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(errorJson),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);
        var request = new DirectoryQueueRequestDto("dir", "out", null, null, null, null, null, null, null, null, null);

        // Act
        var act = () => client.QueueDirectoryAsync(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Technical error message*");
    }
}
