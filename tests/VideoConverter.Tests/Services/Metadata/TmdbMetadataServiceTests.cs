using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoConverter.Web.Features.Metadata.Services;

namespace VideoConverter.Tests.Services.Metadata;

public class TmdbMetadataServiceTests
{
    private const string TmdbSearchJson = """
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
        "overview" : "Film star Vince Chase navigates the vapid terrain of Los Angeles with a close circle of friends and his trusty agent.",
        "popularity" : 27.4097,
        "poster_path" : "/kLKP8zrArBtBboGz3qJOuMGC7rL.jpg",
        "first_air_date" : "2004-07-18",
        "name" : "Entourage",
        "vote_average" : 7.357,
        "vote_count" : 594
      } ],
      "total_pages" : 1,
      "total_results" : 4
    }
    """;

    private const string TmdbEpisodeJson = """
    { "name": "Pilot", "overview": "Pilot episode" }
    """;

    [Fact]
    public async Task GetTvShowMetadataAsync_DeserializesTmdbSearchResult_ForEntourage()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(TmdbSearchJson, TmdbEpisodeJson);
        var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TmdbMetadataService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb:ApiKey"] = "dummy-token"
            })
            .Build();

        var service = new TmdbMetadataService(logger, httpClient, configuration);

        // Act
        var result = await service.GetTvShowMetadataAsync("Entourage", 1, 1, 2004);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.ShowName.Should().Be("Entourage");
        _ = result.FirstAiredYear.Should().Be(2004);
        _ = result.PosterUrl.Should().Be("https://image.tmdb.org/t/p/w500/kLKP8zrArBtBboGz3qJOuMGC7rL.jpg");
        _ = result.EpisodeTitle.Should().Be("Pilot");
        _ = result.Overview.Should().Be("Pilot episode");
    }

    private sealed class StubHttpMessageHandler(string searchJson, string episodeJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Ensure bearer header present (service adds it) to mirror real calls
            var hasAuth = request.Headers.Authorization is { Scheme: "Bearer" };
            if (!hasAuth)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            string body = string.Empty;
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.Contains("/search/tv", StringComparison.OrdinalIgnoreCase))
            {
                body = searchJson;
            }
            else if (path.Contains("/tv/", StringComparison.OrdinalIgnoreCase))
            {
                body = episodeJson;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return Task.FromResult(response);
        }
    }
}
