using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoConverter.Web.Features.Metadata.Dtos;
using VideoConverter.Web.Features.Metadata.Services;

namespace VideoConverter.Tests.Services.Metadata;

public class TmdbSearchServiceTests
{
    private const string MovieSearchJson = """
    {
      "page": 1,
      "results": [
        {
          "adult": false,
          "backdrop_path": "/s3TBrRGB1iav7gFOCNx3H31MoES.jpg",
          "genre_ids": [ 28, 878 ],
          "id": 27205,
          "original_language": "en",
          "original_title": "Inception",
          "overview": "A thief who steals corporate secrets ...",
          "popularity": 100.0,
          "poster_path": "/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg",
          "release_date": "2010-07-16",
          "title": "Inception",
          "video": false,
          "vote_average": 8.4,
          "vote_count": 30000
        }
      ],
      "total_pages": 1,
      "total_results": 1
    }
    """;

    private const string TvSearchJson = """
    {
      "page" : 1,
      "results" : [ {
        "adult" : false,
        "backdrop_path" : "/22uQSlECwD3tcdqiFwo3K3qlCtY.jpg",
        "genre_ids" : [ 35, 18 ],
        "id" : 1940,
        "origin_country" : [ "US" ],
        "original_language" : "en",
        "original_name" : "Entourage",
        "overview" : "Film star Vince Chase navigates ...",
        "popularity" : 27.4097,
        "poster_path" : "/kLKP8zrArBtBboGz3qJOuMGC7rL.jpg",
        "first_air_date" : "2004-07-18",
        "name" : "Entourage",
        "vote_average" : 7.357,
        "vote_count" : 594
      } ],
      "total_pages" : 1,
      "total_results" : 1
    }
    """;

    [Fact]
    public async Task SearchMoviesAsync_MapsResults()
    {
        // Arrange
        var handler = new SearchStubHandler(MovieSearchJson, TvSearchJson);
        using var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TmdbSearchService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb:ApiKey"] = "dummy-token",
            })
            .Build();

        var svc = new TmdbSearchService(logger, httpClient, configuration);

        // Act
        var items = await svc.SearchMoviesAsync("inception", 2010, CancellationToken.None);

        // Assert
        _ = items.Should().NotBeNull();
        _ = items.Count.Should().Be(1);
        var first = items[0];
        _ = first.Should().BeOfType<MovieSearchItemDto>();
        _ = first.Title.Should().Be("Inception");
        _ = first.Year.Should().Be(2010);
        _ = first.PosterUrl.Should().Be("https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg");
        _ = first.Source.Should().Be("tmdb");
        _ = first.TmdbId.Should().Be(27205);
        _ = handler.LastAuthShouldBeBearer.Should().BeTrue();
        _ = handler.LastRequestUri!.AbsolutePath.Should().Contain("/search/movie");
        _ = handler.LastRequestUri!.Query.Should().Contain("year=2010");
    }

    [Fact]
    public async Task SearchTvAsync_MapsResults()
    {
        // Arrange
        var handler = new SearchStubHandler(MovieSearchJson, TvSearchJson);
        using var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TmdbSearchService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb:ApiKey"] = "dummy-token",
            })
            .Build();

        var svc = new TmdbSearchService(logger, httpClient, configuration);

        // Act
        var items = await svc.SearchTvAsync("Entourage", 2004, CancellationToken.None);

        // Assert
        _ = items.Should().NotBeNull();
        _ = items.Count.Should().Be(1);
        var first = items[0];
        _ = first.Should().BeOfType<TvShowSearchItemDto>();
        _ = first.ShowName.Should().Be("Entourage");
        _ = first.FirstAiredYear.Should().Be(2004);
        _ = first.PosterUrl.Should().Be("https://image.tmdb.org/t/p/w500/kLKP8zrArBtBboGz3qJOuMGC7rL.jpg");
        _ = first.Source.Should().Be("tmdb");
        _ = first.TmdbId.Should().Be(1940);
        _ = handler.LastAuthShouldBeBearer.Should().BeTrue();
        _ = handler.LastRequestUri!.AbsolutePath.Should().Contain("/search/tv");
        _ = handler.LastRequestUri!.Query.Should().Contain("first_air_date_year=2004");
    }

    private sealed class SearchStubHandler(string movieJson, string tvJson) : HttpMessageHandler
    {
        public bool LastAuthShouldBeBearer { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastAuthShouldBeBearer = request.Headers.Authorization is { Scheme: "Bearer" };

            string body = string.Empty;
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.Contains("/search/movie", StringComparison.OrdinalIgnoreCase))
            {
                body = movieJson;
            }
            else if (path.Contains("/search/tv", StringComparison.OrdinalIgnoreCase))
            {
                body = tvJson;
            }

            // The caller is responsible for disposing the returned HttpResponseMessage.
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return Task.FromResult(response);
        }
    }
}
