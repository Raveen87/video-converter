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

    private const string TmdbSeason1DetailsJson = """
    {
        "_id": "52571bd519c29571140e52ff",
        "air_date": "2004-07-18",
        "episodes": [
            {
                "air_date": "2004-07-18",
                "episode_number": 1,
                "episode_type": "standard",
                "id": 130868,
                "name": "Entourage",
                "overview": "Basking in the adulation from his latest movie premiere, Hollywood 'it' actor Vince Chase joins childhood pals Eric, Drama and Turtle to debate the pros and cons of attending their tenth high-school reunion back east. Meanwhile, Eric – Vince's de facto manager – incurs the wrath of Vince's agent Ari by advising Vince to pass on a script he hasn't even read. On the domestic-security front, Turtle agrees to test the mettle of the entourage's new guard dog, a Rottweiler named Ahnold.",
                "production_code": "",
                "runtime": null,
                "season_number": 1,
                "show_id": 1940,
                "still_path": "/uIxT23dBItswBgIXGe1Hcwyvd24.jpg",
                "vote_average": 7.4,
                "vote_count": 7
            },
            {
                "air_date": "2004-07-25",
                "episode_number": 2,
                "episode_type": "standard",
                "id": 130858,
                "name": "The Review",
                "overview": "Ignoring the cautionary portents of a negative review, Vince ramps up his already-excessive spending by leasing a new Rolls-Royce. Ari urges Vince to get a \"real\" manager, causing a wounded Eric to do some soul-searching. Meanwhile, Drama skips a \"Vagina Monologues\" rehearsal (go figure) to join Vince and the entourage at a wild party hosted by a sexy actress.",
                "production_code": "",
                "runtime": null,
                "season_number": 1,
                "show_id": 1940,
                "still_path": "/sWKfyeUKKutl6LgKzYMa3ytzVbH.jpg",
                "vote_average": 7.4,
                "vote_count": 8
            }
        ],
        "name": "Season 1",
        "networks": [
            {
                "id": 49,
                "logo_path": "/tuomPhY2UtuPTqqFnKMVHvSb724.png",
                "name": "HBO",
                "origin_country": "US"
            }
        ],
        "overview": "Vince, Eric, Drama, and Turtle begin living it up in the wake of Vince's latest hit, while the boys clash with Ari, attend a slew of wild parties, and decide on Vince's next starring role: an independent film set in Queens and directed by a loose cannon named Billy Walsh.",
        "id": 5633,
        "poster_path": "/uxIB6pCQgelxxcZmIwas6L9YHm0.jpg",
        "season_number": 1,
        "vote_average": 7.6
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
                ["Tmdb:ApiKey"] = "dummy-token",
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

    [Fact]
    public async Task GetTvSeasonDetailsAsync_DeserializesSeasonEpisodes_ForEntourageS1()
    {
        // Arrange
        var handler = new SeasonDetailsStubHttpMessageHandler(TmdbSeason1DetailsJson);
        using var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TmdbMetadataService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb:ApiKey"] = "dummy-token",
            })
            .Build();

        var service = new TmdbMetadataService(logger, httpClient, configuration);

        // Act
        var result = await service.GetTvSeasonDetailsAsync(1940, 1);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Id.Should().Be(5633);
        _ = result.SeasonNumber.Should().Be(1);
        _ = result.Name.Should().Be("Season 1");
        _ = result.Episodes.Should().NotBeNull();
        _ = result.Episodes!.Count.Should().BeGreaterThanOrEqualTo(2);
        _ = result.Episodes![0].EpisodeNumber.Should().Be(1);
        _ = result.Episodes![0].Name.Should().Be("Entourage");
        _ = result.Networks![0].Name.Should().Be("HBO");
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_DeserializesTvShowDetails_ForEntourage()
    {
        // Arrange
        var handler = new ShowDetailsStubHttpMessageHandler(TmdbShowDetailsJson);
        using var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TmdbMetadataService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb:ApiKey"] = "dummy-token",
            })
            .Build();

        var service = new TmdbMetadataService(logger, httpClient, configuration);

        // Act
        var result = await service.GetTvShowDetailsAsync(1940);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Id.Should().Be(1940);
        _ = result.Name.Should().Be("Entourage");
        _ = result.FirstAirDate.Should().Be("2004-07-18");
        _ = result.NumberOfSeasons.Should().Be(8);
        _ = result.NumberOfEpisodes.Should().Be(96);
        _ = result.Genres!.Select(g => g.Name).Should().Contain(["Comedy", "Drama"]);
        _ = result.Networks![0].Name.Should().Be("HBO");
        _ = result.LastEpisodeToAir!.Name.Should().Be("The End");
        _ = result.LastEpisodeToAir.SeasonNumber.Should().Be(8);
        _ = result.LastEpisodeToAir.EpisodeNumber.Should().Be(8);
        _ = result.PosterPath.Should().Be("/kLKP8zrArBtBboGz3qJOuMGC7rL.jpg");
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_RetriesOn429_WithRetryAfterZero_ThenSucceeds()
    {
        // Arrange
        var handler = new Retry429ThenSuccessHandler(TmdbShowDetailsJson, retryAfterSeconds: 0, retriesBeforeSuccess: 2);
        using var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TmdbMetadataService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb:ApiKey"] = "dummy-token",
            })
            .Build();

        var service = new TmdbMetadataService(logger, httpClient, configuration);

        // Act
        var result = await service.GetTvShowDetailsAsync(1940);

        // Assert
        _ = result.Should().NotBeNull();
        _ = handler.Calls.Should().Be(3);
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_RetriesOn429_NoRetryAfter_UsesBackoffWithJitter()
    {
        // Arrange
        var handler = new Retry429ThenSuccessHandler(TmdbShowDetailsJson, retryAfterSeconds: null, retriesBeforeSuccess: 2);
        using var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TmdbMetadataService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb:ApiKey"] = "dummy-token",
            })
            .Build();

        var delays = new List<TimeSpan>();
        Task DelayCapture(TimeSpan d, CancellationToken ct)
        {
            delays.Add(d);
            return Task.CompletedTask;
        }

        var service = new TmdbMetadataService(logger, httpClient, configuration, DelayCapture);

        // Act
        var result = await service.GetTvShowDetailsAsync(1940);

        // Assert
        _ = result.Should().NotBeNull();
        _ = handler.Calls.Should().Be(3);
        _ = delays.Count.Should().Be(2);
        _ = delays.All(d => d > TimeSpan.Zero).Should().BeTrue();
        _ = TimeSpan.FromTicks(delays.Sum(d => d.Ticks)).Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_RetriesOn503_ThenSucceeds()
    {
        // Arrange
        var handler = new Retry503ThenSuccessHandler(TmdbShowDetailsJson, retriesBeforeSuccess: 1);
        using var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TmdbMetadataService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb:ApiKey"] = "dummy-token",
            })
            .Build();

        var delays = new List<TimeSpan>();
        Task DelayCapture(TimeSpan d, CancellationToken ct)
        {
            delays.Add(d);
            return Task.CompletedTask;
        }

        var service = new TmdbMetadataService(logger, httpClient, configuration, DelayCapture);

        // Act
        var result = await service.GetTvShowDetailsAsync(1940);

        // Assert
        _ = result.Should().NotBeNull();
        _ = handler.Calls.Should().Be(2);
        _ = delays.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_MaxRetriesExhausted_ReturnsNull()
    {
        // Arrange
        var handler = new Always429Handler();
        using var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TmdbMetadataService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb:ApiKey"] = "dummy-token",
            })
            .Build();

        var service = new TmdbMetadataService(logger, httpClient, configuration, (_, _) => Task.CompletedTask);

        // Act
        var result = await service.GetTvShowDetailsAsync(1940);

        // Assert
        _ = result.Should().BeNull();
        _ = handler.Calls.Should().Be(6); // 1 initial + 5 retries
    }

    private sealed class Retry429ThenSuccessHandler(string body, int? retryAfterSeconds, int retriesBeforeSuccess) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;

            if (Calls <= retriesBeforeSuccess)
            {
                var r = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                if (retryAfterSeconds is not null)
                {
                    r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds.Value));
                }

                return Task.FromResult(r);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return Task.FromResult(response);
        }
    }

    private sealed class Retry503ThenSuccessHandler(string body, int retriesBeforeSuccess) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;

            if (Calls <= retriesBeforeSuccess)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return Task.FromResult(response);
        }
    }

    private sealed class Always429Handler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            // The caller is responsible for disposing the returned HttpResponseMessage.
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            return Task.FromResult(response);
        }
    }

    private sealed class SeasonDetailsStubHttpMessageHandler(string seasonDetailsJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var hasAuth = request.Headers.Authorization is { Scheme: "Bearer" };
            if (!hasAuth)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            string body = string.Empty;
            if (path.EndsWith("/tv/1940/season/1", StringComparison.OrdinalIgnoreCase))
            {
                body = seasonDetailsJson;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return Task.FromResult(response);
        }
    }

    private sealed class ShowDetailsStubHttpMessageHandler(string showDetailsJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var hasAuth = request.Headers.Authorization is { Scheme: "Bearer" };
            if (!hasAuth)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            string body = string.Empty;
            if (path.EndsWith("/tv/1940", StringComparison.OrdinalIgnoreCase))
            {
                body = showDetailsJson;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return Task.FromResult(response);
        }
    }

    private sealed class StubHttpMessageHandler(string searchJson, string episodeJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
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

            if (string.IsNullOrEmpty(body))
            {
                body = searchJson;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return Task.FromResult(response);
        }
    }
}
