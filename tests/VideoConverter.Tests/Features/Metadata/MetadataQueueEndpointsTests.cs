using VideoConverter.Api.Features.Metadata.Api;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Common.Models;

namespace VideoConverter.Tests.Features.Metadata;

public class MetadataQueueEndpointsTests
{
    [Fact]
    public async Task ResolveBatchMetadataQueueItems_WithEmptyQueueItemIds_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new Mock<IManualResolutionService>();
        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [],
            TmdbId = 123,
            MediaType = MediaType.Movie,
        };

        // Act
        var result = await InvokeResolveBatchMetadataQueueItemsAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>();
        var badRequestResult = (Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>)result;
        _ = badRequestResult.Value.Should().Be("At least one queue item ID is required.");
        mockService.Verify(s => s.ResolveBatchMetadataQueueItemsAsync(It.IsAny<ResolveBatchMetadataQueueRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveBatchMetadataQueueItems_WithNullQueueItemIds_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new Mock<IManualResolutionService>();
        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = null!,
            TmdbId = 123,
            MediaType = MediaType.Movie,
        };

        // Act
        var result = await InvokeResolveBatchMetadataQueueItemsAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>();
        mockService.Verify(s => s.ResolveBatchMetadataQueueItemsAsync(It.IsAny<ResolveBatchMetadataQueueRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveBatchMetadataQueueItems_WithValidQueueItemIds_CallsService()
    {
        // Arrange
        var mockService = new Mock<IManualResolutionService>();
        mockService
            .Setup(s => s.ResolveBatchMetadataQueueItemsAsync(It.IsAny<ResolveBatchMetadataQueueRequestDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [1, 2, 3],
            TmdbId = 123,
            MediaType = MediaType.Movie,
        };

        // Act
        var result = await InvokeResolveBatchMetadataQueueItemsAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok>();
        mockService.Verify(s => s.ResolveBatchMetadataQueueItemsAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Helper method to invoke the private static endpoint method via reflection
    private static async Task<IResult> InvokeResolveBatchMetadataQueueItemsAsync(
        ResolveBatchMetadataQueueRequestDto request,
        IManualResolutionService service,
        CancellationToken cancellationToken)
    {
        var methodInfo = typeof(ManualResolutionEndpoints).GetMethod(
            "ResolveBatchMetadataQueueItems",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        _ = methodInfo.Should().NotBeNull("ResolveBatchMetadataQueueItems method should exist");

        var task = (Task<IResult>)methodInfo!.Invoke(null, [request, service, cancellationToken])!;
        return await task.ConfigureAwait(false);
    }
}
