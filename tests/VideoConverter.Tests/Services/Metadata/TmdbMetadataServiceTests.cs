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

    private const string TmdbShowDetailsJson = """
    {
        "adult": false,
        "backdrop_path": "/22uQSlECwD3tcdqiFwo3K3qlCtY.jpg",
        "created_by": [
            {
                "id": 183036,
                "credit_id": "5522f1c1c3a3684dd4007183",
                "name": "Doug Ellin",
                "original_name": "Doug Ellin",
                "gender": 2,
                "profile_path": "/7jugVaVPHR6o1Gjtahuxz1ohZsw.jpg"
            }
        ],
        "episode_run_time": [],
        "first_air_date": "2004-07-18",
        "genres": [
            {
                "id": 35,
                "name": "Comedy"
            },
            {
                "id": 18,
                "name": "Drama"
            }
        ],
        "homepage": "http://www.hbo.com/entourage/index.html",
        "id": 1940,
        "in_production": false,
        "languages": [
            "en"
        ],
        "last_air_date": "2011-09-11",
        "last_episode_to_air": {
            "id": 131084,
            "name": "The End",
            "overview": "Vince takes a surprising step after a first date with Sophia. A therapy session yields mixed results for Ari and Mrs. Ari, and Vince, Drama and Turtle urge Sloan to work things out with Eric.",
            "vote_average": 8.9,
            "vote_count": 7,
            "air_date": "2011-09-11",
            "episode_number": 8,
            "episode_type": "finale",
            "production_code": "",
            "runtime": null,
            "season_number": 8,
            "show_id": 1940,
            "still_path": "/oBpBkmfqDHV2Klbqo5BidWp2qvK.jpg"
        },
        "name": "Entourage",
        "next_episode_to_air": null,
        "networks": [
            {
                "id": 49,
                "logo_path": "/tuomPhY2UtuPTqqFnKMVHvSb724.png",
                "name": "HBO",
                "origin_country": "US"
            }
        ],
        "number_of_episodes": 96,
        "number_of_seasons": 8,
        "origin_country": [
            "US"
        ],
        "original_language": "en",
        "original_name": "Entourage",
        "overview": "Film star Vince Chase navigates the vapid terrain of Los Angeles with a close circle of friends and his trusty agent.",
        "popularity": 27.4097,
        "poster_path": "/kLKP8zrArBtBboGz3qJOuMGC7rL.jpg",
        "production_companies": [
            {
                "id": 8536,
                "logo_path": null,
                "name": "Leverage Entertainment",
                "origin_country": "US"
            },
            {
                "id": 8537,
                "logo_path": null,
                "name": "Closest to the Hole Productions",
                "origin_country": "US"
            }
        ],
        "production_countries": [
            {
                "iso_3166_1": "US",
                "name": "United States of America"
            }
        ],
        "seasons": [
            {
                "air_date": null,
                "episode_count": 16,
                "id": 5641,
                "name": "Specials",
                "overview": "",
                "poster_path": "/un326VuwwqIAupu5c9VpXWN8JUL.jpg",
                "season_number": 0,
                "vote_average": 0.0
            },
            {
                "air_date": "2004-07-18",
                "episode_count": 8,
                "id": 5633,
                "name": "Season 1",
                "overview": "Vince, Eric, Drama, and Turtle begin living it up in the wake of Vince's latest hit, while the boys clash with Ari, attend a slew of wild parties, and decide on Vince's next starring role: an independent film set in Queens and directed by a loose cannon named Billy Walsh.",
                "poster_path": "/uxIB6pCQgelxxcZmIwas6L9YHm0.jpg",
                "season_number": 1,
                "vote_average": 7.6
            }
        ],
        "spoken_languages": [
            {
                "english_name": "English",
                "iso_639_1": "en",
                "name": "English"
            }
        ],
        "status": "Ended",
        "tagline": "A lifestyle is a terrible thing to waste.",
        "type": "Scripted",
        "vote_average": 7.357,
        "vote_count": 594
    }
    """;

    [Fact]
    public async Task GetTvShowMetadataAsync_DeserializesTmdbSearchResult_ForEntourage()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(TmdbSearchJson, TmdbEpisodeJson);
        using var httpClient = new HttpClient(handler);
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

            using var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return Task.FromResult(response);
        }
    }
}
