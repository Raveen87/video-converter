using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using VideoConverter.Common.Models;
using VideoConverter.Web.Services;
using Xunit;

namespace VideoConverter.Web.Tests.Services;

public sealed class VideoConverterApiClientBatchTests
{
    [Fact]
    public async Task GetBatchStatusAsync_WithValidBatchId_ReturnsStatus()
    {
        // Arrange
        var batchId = 42;
        var expectedResponse = new DirectoryBatchStatusDto(
            batchId,
            "Processing",
            10,
            5,
            5,
            "/path/to/current.mp4",
            DateTime.UtcNow,
            1);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().EndsWith($"api/conversions/queue-directory/batches/{batchId}", StringComparison.Ordinal)),
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

        // Act
        var result = await client.GetBatchStatusAsync(batchId);

        // Assert
        result.Should().NotBeNull();
        result.BatchId.Should().Be(batchId);
        result.Status.Should().Be("Processing");
        result.TotalItems.Should().Be(10);
        result.ProcessedItems.Should().Be(5);
        result.UnprocessedItems.Should().Be(5);
        result.CurrentItemPath.Should().Be("/path/to/current.mp4");
        result.ProfileId.Should().Be(1);
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().EndsWith($"api/conversions/queue-directory/batches/{batchId}", StringComparison.Ordinal)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetBatchStatusAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        var batchId = 42;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not Found"),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        // Act
        var act = () => client.GetBatchStatusAsync(batchId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetBatchStatusAsync_WithNullResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var batchId = 42;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null"),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        // Act
        var act = () => client.GetBatchStatusAsync(batchId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Failed to deserialize batch status response for batch ID {batchId}*");
    }

    [Fact]
    public async Task GetAllBatchesAsync_WithoutStatusFilter_ReturnsAllBatches()
    {
        // Arrange
        var expectedResponse = new List<DirectoryBatchStatusDto>
        {
            new DirectoryBatchStatusDto(1, "Completed", 10, 10, 0, null, DateTime.UtcNow, 1),
            new DirectoryBatchStatusDto(2, "Processing", 5, 3, 2, "/path/to/file.mp4", DateTime.UtcNow, 2),
            new DirectoryBatchStatusDto(3, "Pending", 8, 0, 8, null, DateTime.UtcNow, null),
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().EndsWith("api/conversions/queue-directory/batches", StringComparison.Ordinal)),
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

        // Act
        var result = await client.GetAllBatchesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].BatchId.Should().Be(1);
        result[1].BatchId.Should().Be(2);
        result[2].BatchId.Should().Be(3);
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().EndsWith("api/conversions/queue-directory/batches", StringComparison.Ordinal)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAllBatchesAsync_WithStatusFilter_ReturnsFilteredBatches()
    {
        // Arrange
        var status = "Processing";
        var expectedResponse = new List<DirectoryBatchStatusDto>
        {
            new DirectoryBatchStatusDto(2, "Processing", 5, 3, 2, "/path/to/file.mp4", DateTime.UtcNow, 2),
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("api/conversions/queue-directory/batches?status=") &&
                    req.RequestUri!.ToString().Contains(Uri.EscapeDataString(status))),
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

        // Act
        var result = await client.GetAllBatchesAsync(status);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Status.Should().Be("Processing");
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().Contains("api/conversions/queue-directory/batches?status=") &&
                req.RequestUri!.ToString().Contains(Uri.EscapeDataString(status))),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAllBatchesAsync_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var expectedResponse = Array.Empty<DirectoryBatchStatusDto>();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
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

        // Act
        var result = await client.GetAllBatchesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllBatchesAsync_WithNullResponse_ReturnsEmptyList()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null"),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        // Act
        var result = await client.GetAllBatchesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllBatchesAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server Error"),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        // Act
        var act = () => client.GetAllBatchesAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetAllBatchesAsync_WithWhitespaceStatus_UsesNoFilter()
    {
        // Arrange
        var expectedResponse = new List<DirectoryBatchStatusDto>
        {
            new DirectoryBatchStatusDto(1, "Completed", 10, 10, 0, null, DateTime.UtcNow, 1),
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().EndsWith("api/conversions/queue-directory/batches", StringComparison.Ordinal) &&
                    !req.RequestUri!.ToString().Contains("?status=")),
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

        // Act
        var result = await client.GetAllBatchesAsync("   ");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().EndsWith("api/conversions/queue-directory/batches", StringComparison.Ordinal) &&
                !req.RequestUri!.ToString().Contains("?status=")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAllBatchesAsync_WithSpecialCharactersInStatus_EscapesCorrectly()
    {
        // Arrange
        var status = "Processing & Pending";
        var expectedResponse = new List<DirectoryBatchStatusDto>
        {
            new DirectoryBatchStatusDto(1, status, 10, 5, 5, null, DateTime.UtcNow, 1),
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
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

        // Act
        var result = await client.GetAllBatchesAsync(status);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().Contains("api/conversions/queue-directory/batches?status=") &&
                req.RequestUri!.ToString().Contains("%26")),
            ItExpr.IsAny<CancellationToken>());
    }
}
