using VideoConverter.Api.Features.Metadata.Api;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Common.Models;

namespace VideoConverter.Tests.Features.Metadata;

public class MetadataQueueEndpointsTests
{
    [Fact]
    public async Task ResolveBatchQueueItems_WithEmptyQueueItemIds_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new Mock<IMetadataResolutionService>();
        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [],
            TmdbId = 123,
            MediaType = MediaType.Movie,
        };

        // Act
        var result = await InvokeResolveBatchQueueItemsAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>();
        var badRequestResult = (Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>)result;
        _ = badRequestResult.Value.Should().Be("At least one queue item ID is required.");
        mockService.Verify(s => s.ResolveBatchQueueItemsAsync(It.IsAny<ResolveBatchMetadataQueueRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveBatchQueueItems_WithNullQueueItemIds_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new Mock<IMetadataResolutionService>();
        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = null!,
            TmdbId = 123,
            MediaType = MediaType.Movie,
        };

        // Act
        var result = await InvokeResolveBatchQueueItemsAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>();
        mockService.Verify(s => s.ResolveBatchQueueItemsAsync(It.IsAny<ResolveBatchMetadataQueueRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveBatchQueueItems_WithValidQueueItemIds_CallsService()
    {
        // Arrange
        var mockService = new Mock<IMetadataResolutionService>();
        mockService
            .Setup(s => s.ResolveBatchQueueItemsAsync(It.IsAny<ResolveBatchMetadataQueueRequestDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [1, 2, 3],
            TmdbId = 123,
            MediaType = MediaType.Movie,
        };

        // Act
        var result = await InvokeResolveBatchQueueItemsAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok>();
        mockService.Verify(s => s.ResolveBatchQueueItemsAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Helper method to invoke the private static endpoint method via reflection
    private static async Task<IResult> InvokeResolveBatchQueueItemsAsync(
        ResolveBatchMetadataQueueRequestDto request,
        IMetadataResolutionService service,
        CancellationToken cancellationToken)
    {
        var methodInfo = typeof(MetadataQueueEndpoints).GetMethod(
            "ResolveBatchQueueItems",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        _ = methodInfo.Should().NotBeNull("ResolveBatchQueueItems method should exist");

        var task = (Task<IResult>)methodInfo!.Invoke(null, [request, service, cancellationToken])!;
        return await task.ConfigureAwait(false);
    }
}
