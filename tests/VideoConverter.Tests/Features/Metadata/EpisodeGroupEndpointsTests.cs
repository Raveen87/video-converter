using VideoConverter.Api.Features.Metadata.Api;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Common.Models;

namespace VideoConverter.Tests.Features.Metadata;

public class EpisodeGroupEndpointsTests
{
    [Fact]
    public async Task GetEpisodeGroups_WithValidShowId_ReturnsOkWithGroups()
    {
        // Arrange
        var mockService = new Mock<IEpisodeGroupService>();
        var expectedGroups = new List<EpisodeGroupDto>
        {
            new()
            {
                Id = "group-1",
                Name = "DVD Order",
                Type = "DVD",
                Description = "DVD release order",
                GroupCount = 2,
                EpisodeCount = 24,
            },
            new()
            {
                Id = "group-2",
                Name = "Absolute Order",
                Type = "Absolute",
                Description = null,
                GroupCount = 1,
                EpisodeCount = 24,
            },
        };

        mockService
            .Setup(s => s.GetEpisodeGroupsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroups);

        // Act
        var result = await InvokeGetEpisodeGroupsAsync(12345, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<IEnumerable<EpisodeGroupDto>>>();
        var okResult = (Microsoft.AspNetCore.Http.HttpResults.Ok<IEnumerable<EpisodeGroupDto>>)result;
        _ = okResult.Value.Should().BeEquivalentTo(expectedGroups);
        mockService.Verify(s => s.GetEpisodeGroupsAsync(12345, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEpisodeGroups_WithNoGroups_ReturnsOkWithEmptyList()
    {
        // Arrange
        var mockService = new Mock<IEpisodeGroupService>();
        mockService
            .Setup(s => s.GetEpisodeGroupsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await InvokeGetEpisodeGroupsAsync(12345, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<IEnumerable<EpisodeGroupDto>>>();
        var okResult = (Microsoft.AspNetCore.Http.HttpResults.Ok<IEnumerable<EpisodeGroupDto>>)result;
        _ = okResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEpisodeGroupDetails_WithValidGroupId_ReturnsOkWithDetails()
    {
        // Arrange
        var mockService = new Mock<IEpisodeGroupService>();
        var expectedDetails = new EpisodeGroupDetailsDto
        {
            Id = "group-1",
            Name = "DVD Order",
            Type = "DVD",
            Description = "DVD release order",
            Seasons =
            [
                new EpisodeGroupSeasonDto
                {
                    Order = 1,
                    Name = "Season 1",
                    Episodes =
                    [
                        new EpisodeGroupEpisodeDto
                        {
                            TmdbEpisodeId = 101,
                            Order = 1,
                            Name = "Pilot",
                            Overview = "The first episode",
                            OriginalSeason = 1,
                            OriginalEpisode = 1,
                        },
                        new EpisodeGroupEpisodeDto
                        {
                            TmdbEpisodeId = 102,
                            Order = 2,
                            Name = "Second Episode",
                            Overview = "The second episode",
                            OriginalSeason = 1,
                            OriginalEpisode = 2,
                        },
                    ],
                },
            ],
        };

        mockService
            .Setup(s => s.GetEpisodeGroupDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDetails);

        // Act
        var result = await InvokeGetEpisodeGroupDetailsAsync(12345, "group-1", mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<EpisodeGroupDetailsDto>>();
        var okResult = (Microsoft.AspNetCore.Http.HttpResults.Ok<EpisodeGroupDetailsDto>)result;
        _ = okResult.Value.Should().BeEquivalentTo(expectedDetails);
        mockService.Verify(s => s.GetEpisodeGroupDetailsAsync("group-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEpisodeGroupDetails_WithNonExistentGroupId_ReturnsNotFound()
    {
        // Arrange
        var mockService = new Mock<IEpisodeGroupService>();
        mockService
            .Setup(s => s.GetEpisodeGroupDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EpisodeGroupDetailsDto?)null);

        // Act
        var result = await InvokeGetEpisodeGroupDetailsAsync(12345, "non-existent-group", mockService.Object, CancellationToken.None);

        // Assert - Results.NotFound with anonymous object returns JsonHttpResult
        _ = result.GetType().Name.Should().Contain("NotFound");
        mockService.Verify(s => s.GetEpisodeGroupDetailsAsync("non-existent-group", It.IsAny<CancellationToken>()), Times.Once);
    }

    // Helper method to invoke the private static GetEpisodeGroups endpoint method via reflection
    private static async Task<IResult> InvokeGetEpisodeGroupsAsync(
        int showId,
        IEpisodeGroupService service,
        CancellationToken cancellationToken)
    {
        var methodInfo = typeof(EpisodeGroupApi).GetMethod(
            "GetEpisodeGroups",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        _ = methodInfo.Should().NotBeNull("GetEpisodeGroups method should exist");

        var task = (Task<IResult>)methodInfo!.Invoke(null, [showId, service, cancellationToken])!;
        return await task.ConfigureAwait(false);
    }

    // Helper method to invoke the private static GetEpisodeGroupDetails endpoint method via reflection
    private static async Task<IResult> InvokeGetEpisodeGroupDetailsAsync(
        int showId,
        string groupId,
        IEpisodeGroupService service,
        CancellationToken cancellationToken)
    {
        var methodInfo = typeof(EpisodeGroupApi).GetMethod(
            "GetEpisodeGroupDetails",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        _ = methodInfo.Should().NotBeNull("GetEpisodeGroupDetails method should exist");

        var task = (Task<IResult>)methodInfo!.Invoke(null, [showId, groupId, service, cancellationToken])!;
        return await task.ConfigureAwait(false);
    }
}
