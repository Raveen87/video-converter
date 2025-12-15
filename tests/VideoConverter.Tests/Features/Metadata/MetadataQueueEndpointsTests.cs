using Microsoft.AspNetCore.Http;
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
    public async Task ResolveMetadataQueueItem_WithEpisodeGroupId_PassesGroupIdToService()
    {
        // Arrange
        var mockService = new Mock<IManualResolutionService>();
        var request = new ResolveMetadataRequestDto
        {
            TmdbId = 456,
            MediaType = MediaType.TvShow,
            EpisodeGroupId = "5b1c3b3dc3a3684e0c0f0e9a",
        };

        mockService
            .Setup(s => s.ResolveMetadataQueueItemAsync(It.IsAny<int>(), It.IsAny<ResolveMetadataRequestDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await InvokeResolveMetadataQueueItemAsync(1, request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok>();
        mockService.Verify(
            s => s.ResolveMetadataQueueItemAsync(
                1,
                It.Is<ResolveMetadataRequestDto>(r => r.EpisodeGroupId == "5b1c3b3dc3a3684e0c0f0e9a"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveBatchMetadataQueueItems_WithPartialFailures_Returns207MultiStatus()
    {
        // Arrange
        var mockService = new Mock<IManualResolutionService>();
        var partialResult = new BatchResolutionResultDto
        {
            SuccessfulIds = [1, 2],
            Errors =
            [
                new BatchResolutionErrorDto { QueueItemId = 3, ErrorMessage = "Episode not found in group" },
            ],
        };
        mockService
            .Setup(s => s.ResolveBatchMetadataQueueItemsAsync(It.IsAny<ResolveBatchMetadataQueueRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partialResult);

        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [1, 2, 3],
            TmdbId = 456,
            MediaType = MediaType.TvShow,
            EpisodeGroupId = "5b1c3b3dc3a3684e0c0f0e9a",
        };

        // Act
        var result = await InvokeResolveBatchMetadataQueueItemsAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<BatchResolutionResultDto>>();
        var jsonResult = (Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<BatchResolutionResultDto>)result;
        _ = jsonResult.StatusCode.Should().Be(StatusCodes.Status207MultiStatus);
        _ = jsonResult.Value.Should().NotBeNull();
        _ = jsonResult.Value!.SuccessfulIds.Should().BeEquivalentTo([1, 2]);
        _ = jsonResult.Value.Errors.Should().HaveCount(1);
        _ = jsonResult.Value.Errors[0].QueueItemId.Should().Be(3);
    }

    [Fact]
    public async Task ResolveBatchMetadataQueueItems_WithAllSuccessful_Returns200Ok()
    {
        // Arrange
        var mockService = new Mock<IManualResolutionService>();
        var successResult = new BatchResolutionResultDto
        {
            SuccessfulIds = [1, 2, 3],
            Errors = [],
        };
        mockService
            .Setup(s => s.ResolveBatchMetadataQueueItemsAsync(It.IsAny<ResolveBatchMetadataQueueRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [1, 2, 3],
            TmdbId = 456,
            MediaType = MediaType.TvShow,
            EpisodeGroupId = "5b1c3b3dc3a3684e0c0f0e9a",
        };

        // Act
        var result = await InvokeResolveBatchMetadataQueueItemsAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<BatchResolutionResultDto>>();
        var okResult = (Microsoft.AspNetCore.Http.HttpResults.Ok<BatchResolutionResultDto>)result;
        _ = okResult.Value.Should().NotBeNull();
        _ = okResult.Value!.SuccessfulIds.Should().BeEquivalentTo([1, 2, 3]);
        _ = okResult.Value.Errors.Should().BeEmpty();
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
        var expectedResult = new BatchResolutionResultDto
        {
            SuccessfulIds = [1, 2, 3],
            Errors = [],
        };
        mockService
            .Setup(s => s.ResolveBatchMetadataQueueItemsAsync(It.IsAny<ResolveBatchMetadataQueueRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var request = new ResolveBatchMetadataQueueRequestDto
        {
            QueueItemIds = [1, 2, 3],
            TmdbId = 123,
            MediaType = MediaType.Movie,
        };

        // Act
        var result = await InvokeResolveBatchMetadataQueueItemsAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<BatchResolutionResultDto>>();
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

    private static async Task<IResult> InvokeResolveMetadataQueueItemAsync(
        int id,
        ResolveMetadataRequestDto request,
        IManualResolutionService service,
        CancellationToken cancellationToken)
    {
        var methodInfo = typeof(ManualResolutionEndpoints).GetMethod(
            "ResolveMetadataQueueItem",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        _ = methodInfo.Should().NotBeNull("ResolveMetadataQueueItem method should exist");

        var task = (Task<IResult>)methodInfo!.Invoke(null, [id, request, service, cancellationToken])!;
        return await task.ConfigureAwait(false);
    }
}
