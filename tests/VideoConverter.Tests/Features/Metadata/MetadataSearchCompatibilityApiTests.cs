using System.Linq;

using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Moq;

using VideoConverter.Api.Features.Metadata.Api;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

using Xunit;

namespace VideoConverter.Tests.Features.Metadata;

public class MetadataSearchCompatibilityApiTests
{
    [Fact]
    public async Task SearchMetadata_WithMissingQuery_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new Mock<IMetadataService>();
        var request = new MetadataSearchRequestDto { Query = string.Empty };

        // Act
        var result = await InvokeSearchMetadataAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>();
        var badRequestResult = (Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>)result;
        _ = badRequestResult.Value.Should().Be("Query is required.");
        mockService.Verify(s => s.SearchMovieAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        mockService.Verify(s => s.SearchTvShowAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMetadata_WithMovieType_UsesMovieSearchOnly()
    {
        // Arrange
        var mockService = new Mock<IMetadataService>();
        var movieResults = new[]
        {
            new MetadataSearchResultDto
            {
                Id = 1,
                Title = "Inception",
                Year = 2010,
                Type = MediaType.Movie,
            },
        };

        mockService
            .Setup(s => s.SearchMovieAsync("inception", 2010, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movieResults);

        var request = new MetadataSearchRequestDto
        {
            Query = "inception",
            Year = 2010,
            Type = MediaType.Movie,
        };

        // Act
        var result = await InvokeSearchMetadataAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<IEnumerable<MetadataSearchResultDto>>>();
        var okResult = (Microsoft.AspNetCore.Http.HttpResults.Ok<IEnumerable<MetadataSearchResultDto>>)result;
        _ = okResult.Value.Should().BeEquivalentTo(movieResults);
        mockService.Verify(s => s.SearchMovieAsync("inception", 2010, It.IsAny<CancellationToken>()), Times.Once);
        mockService.Verify(s => s.SearchTvShowAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMetadata_WithNoType_SearchesBothAndOrdersByYearDescending()
    {
        // Arrange
        var mockService = new Mock<IMetadataService>();
        var movieResults = new[]
        {
            new MetadataSearchResultDto
            {
                Id = 1,
                Title = "Older Movie",
                Year = 2001,
                Type = MediaType.Movie,
            },
        };
        var tvResults = new[]
        {
            new MetadataSearchResultDto
            {
                Id = 2,
                Title = "Newer Show",
                Year = 2003,
                Type = MediaType.TvShow,
            },
        };

        mockService
            .Setup(s => s.SearchMovieAsync("query", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movieResults);
        mockService
            .Setup(s => s.SearchTvShowAsync("query", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tvResults);

        var request = new MetadataSearchRequestDto
        {
            Query = "query",
        };

        // Act
        var result = await InvokeSearchMetadataAsync(request, mockService.Object, CancellationToken.None);

        // Assert
        _ = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<IEnumerable<MetadataSearchResultDto>>>();
        var okResult = (Microsoft.AspNetCore.Http.HttpResults.Ok<IEnumerable<MetadataSearchResultDto>>)result;
        _ = okResult.Value.Should().ContainInOrder(tvResults[0], movieResults[0]);
        mockService.Verify(s => s.SearchMovieAsync("query", null, It.IsAny<CancellationToken>()), Times.Once);
        mockService.Verify(s => s.SearchTvShowAsync("query", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void MapMetadataEndpoints_MapsLegacyPostMetadataSearchRoute()
    {
        // Arrange
        var app = BuildRouteApp();

        // Act
        app.MapMetadataEndpoints();

        // Assert
        IEndpointRouteBuilder routeBuilder = app;
        var endpoints = routeBuilder.DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        var routeDebug = endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => new
            {
                Route = endpoint.RoutePattern.RawText,
                Methods = endpoint.Metadata.OfType<HttpMethodMetadata>().SelectMany(metadata => metadata.HttpMethods).ToArray(),
            })
            .ToArray();

        _ = routeDebug.Should().NotBeEmpty();

        var hasLegacyPostRoute = endpoints
            .Any(endpoint =>
                (string.Equals(endpoint.RoutePattern.RawText, "api/metadata/search", StringComparison.Ordinal)
                || string.Equals(endpoint.RoutePattern.RawText, "/api/metadata/search", StringComparison.Ordinal))
                && endpoint.Metadata.OfType<HttpMethodMetadata>().Any(methods => methods.HttpMethods.Contains("POST", StringComparer.OrdinalIgnoreCase)));

        _ = hasLegacyPostRoute.Should().BeTrue();
    }

    [Fact]
    public void MapMetadataEndpoints_PreservesGetMetadataSearchRoutes()
    {
        // Arrange
        var app = BuildRouteApp();

        // Act
        app.MapControllers();
        app.MapMetadataEndpoints();

        // Assert
        IEndpointRouteBuilder routeBuilder = app;
        var endpoints = routeBuilder.DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        var routeDebug = endpoints
            .Select(endpoint => new
            {
                Route = endpoint.RoutePattern.RawText,
                Methods = endpoint.Metadata.OfType<HttpMethodMetadata>().SelectMany(metadata => metadata.HttpMethods).ToArray(),
            })
            .ToArray();

        _ = routeDebug.Should().NotBeEmpty();

        var hasMoviesGetRoute = endpoints
            .Any(endpoint =>
                string.Equals(endpoint.RoutePattern.RawText, "api/metadata/search/movies", StringComparison.Ordinal)
                && endpoint.Metadata.OfType<HttpMethodMetadata>().Any(methods => methods.HttpMethods.Contains("GET", StringComparer.OrdinalIgnoreCase)));

        var hasTvGetRoute = endpoints
            .Any(endpoint =>
                string.Equals(endpoint.RoutePattern.RawText, "api/metadata/search/tv", StringComparison.Ordinal)
                && endpoint.Metadata.OfType<HttpMethodMetadata>().Any(methods => methods.HttpMethods.Contains("GET", StringComparer.OrdinalIgnoreCase)));

        _ = hasMoviesGetRoute.Should().BeTrue();
        _ = hasTvGetRoute.Should().BeTrue();
    }

    private static async Task<IResult> InvokeSearchMetadataAsync(
        MetadataSearchRequestDto request,
        IMetadataService service,
        CancellationToken cancellationToken)
    {
        var methodInfo = typeof(MetadataSearchCompatibilityApi).GetMethod(
            "SearchMetadata",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        _ = methodInfo.Should().NotBeNull("SearchMetadata method should exist");

        var task = (Task<IResult>)methodInfo!.Invoke(null, [request, service, cancellationToken])!;
        return await task.ConfigureAwait(false);
    }

    private static WebApplication BuildRouteApp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });

        _ = builder.Services.AddControllers().AddApplicationPart(typeof(MetadataSearchApi).Assembly);
        _ = builder.Services.AddLogging();
        _ = builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        _ = builder.Services.AddSingleton<IManualResolutionService>(new Mock<IManualResolutionService>().Object);
        _ = builder.Services.AddSingleton<IEpisodeGroupService>(new Mock<IEpisodeGroupService>().Object);
        _ = builder.Services.AddSingleton<IMetadataService>(new Mock<IMetadataService>().Object);

        return builder.Build();
    }
}
