using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using VideoConverter.Web.Services;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Tests;

public class ApiTests(WebApplicationFactory<Web.Program> factory) : IClassFixture<WebApplicationFactory<Web.Program>>
{
    private static string CreateTempDbPath()
    {
        return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) + ".db";
    }

    private readonly WebApplicationFactory<Web.Program> _factory =
  factory.WithWebHostBuilder(builder =>
      {
          _ = builder.UseSetting("ConnectionStrings:Default", $"Data Source={CreateTempDbPath()}");
          _ = builder.ConfigureServices(services =>
          {
              // Replace real conversion runner with a fake for tests
              var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConversionRunner));
              if (descriptor != null)
              {
                  _ = services.Remove(descriptor);
              }
              _ = services.AddSingleton<IConversionRunner, FakeConversionRunner>();

              // Replace real media prober with a fake that we control per test
              var proberDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaProber));
              if (proberDescriptor != null)
              {
                  _ = services.Remove(proberDescriptor);
              }
              _ = services.AddSingleton<FakeMediaProber>();
              _ = services.AddSingleton<IMediaProber>(sp => sp.GetRequiredService<FakeMediaProber>());
          });
      });

    [Fact]
    public async Task Post_creates_row_in_sqlite()
    {
        var client = _factory.CreateClient();

        // Create real temp input file and output directory to satisfy validation
        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllTextAsync(tempInput, "dummy");
        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir };

        var response = await client.PostAsJsonAsync("/api/conversions", payload);
        _ = response.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<ConversionDto>>("/api/conversions");
        _ = list.Should().NotBeNull();
        _ = list!.Should().ContainSingle();
        var item = list!.Single();
        _ = item.InputPath.Should().Be(payload.inputPath);
        _ = item.OutputDirectory.Should().Be(payload.outputDirectory);
    }

    [Fact]
    public async Task Post_persists_crf_bitrate_and_resolution()
    {
        var client = _factory.CreateClient();

        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllTextAsync(tempInput, "dummy");
        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir, crf = 22, audioBitrateKbps = 192 };

        var response = await client.PostAsJsonAsync("/api/conversions", payload);
        _ = response.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<ConversionDto>>("/api/conversions");
        _ = list.Should().NotBeNull();
        var created = list!.Single(x => x.InputPath == payload.inputPath);
        _ = created.Crf.Should().Be(22);
        _ = created.AudioBitrateKbps.Should().Be(192);
        _ = created.Resolution.Should().BeNull();
    }

    [Fact]
    public async Task Post_with_audio_languages_persists_selected_indices_and_propagates()
    {
        var factory = new WebApplicationFactory<Web.Program>().WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ConnectionStrings:Default", $"Data Source={CreateTempDbPath()}");
            _ = builder.UseSetting("Ffmpeg:DryRun", "true");
            _ = builder.ConfigureServices(services =>
            {
                var proberDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaProber));
                if (proberDescriptor != null)
                {
                    _ = services.Remove(proberDescriptor);
                }
                _ = services.AddSingleton<FakeMediaProber>();
                _ = services.AddSingleton<IMediaProber>(sp => sp.GetRequiredService<FakeMediaProber>());
            });
        });

        var client = factory.CreateClient();

        // Configure fake probe data: two audio streams eng and swe
        var fakeProber = factory.Services.GetRequiredService<FakeMediaProber>();
        fakeProber.Streams =
        [
            new MediaStream { Index = 1, CodecType = "audio", LanguageTag = "eng", Title = "English" },
            new MediaStream { Index = 2, CodecType = "audio", LanguageTag = "swe", Title = "Swedish" },
        ];

        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllTextAsync(tempInput, "dummy");
        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir, audioLanguages = new[] { "English" } };

        var response = await client.PostAsJsonAsync("/api/conversions", payload);
        _ = response.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<ConversionDto>>("/api/conversions");
        var created = list!.Single(x => x.InputPath == payload.inputPath);

        // Start conversion and inspect command: should map 0:1 for audio
        var startResp = await client.PostAsync($"/api/conversions/{created.Id}/start", content: null);
        _ = startResp.EnsureSuccessStatusCode();
        var startPayload = await startResp.Content.ReadFromJsonAsync<RealRunnerTests.StartResponse>();
        _ = startPayload!.Command.Should().Contain("-map 0:1");
    }

    [Fact]
    public async Task Post_with_subtitle_languages_persists_selected_indices_and_propagates()
    {
        var factory = new WebApplicationFactory<Web.Program>().WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ConnectionStrings:Default", $"Data Source={CreateTempDbPath()}");
            _ = builder.UseSetting("Ffmpeg:DryRun", "true");
            _ = builder.ConfigureServices(services =>
            {
                var proberDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaProber));
                if (proberDescriptor != null)
                {
                    _ = services.Remove(proberDescriptor);
                }
                _ = services.AddSingleton<FakeMediaProber>();
                _ = services.AddSingleton<IMediaProber>(sp => sp.GetRequiredService<FakeMediaProber>());
            });
        });

        var client = factory.CreateClient();

        var fakeProber = factory.Services.GetRequiredService<FakeMediaProber>();
        fakeProber.Streams =
        [
            new MediaStream { Index = 0, CodecType = "video", LanguageTag = null, Title = null },
            new MediaStream { Index = 1, CodecType = "audio", LanguageTag = "eng", Title = "English" },
            new MediaStream { Index = 3, CodecType = "subtitle", LanguageTag = "eng", Title = "English CC" },
            new MediaStream { Index = 4, CodecType = "subtitle", LanguageTag = "swe", Title = "Svenska" },
        ];

        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mkv");
        await File.WriteAllTextAsync(tempInput, "dummy");
        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir, subtitleLanguages = new[] { "Swedish" } };

        var create = await client.PostAsJsonAsync("/api/conversions", payload);
        _ = create.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<ConversionDto>>("/api/conversions");
        var created = list!.Single(x => x.InputPath == payload.inputPath);

        var startResp = await client.PostAsync($"/api/conversions/{created.Id}/start", content: null);
        _ = startResp.EnsureSuccessStatusCode();
        var startPayload = await startResp.Content.ReadFromJsonAsync<RealRunnerTests.StartResponse>();
        // Should map subtitles using index 4
        _ = startPayload!.Command.Should().Contain("-map 0:4");
        // And set -c:s copy
        _ = startPayload!.Command.Should().Contain("-c:s copy");
    }

    [Fact]
    public async Task Post_with_invalid_resolution_returns_validation_error()
    {
        var client = _factory.CreateClient();

        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllTextAsync(tempInput, "dummy");
        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir, resolution = 999 };
        var response = await client.PostAsJsonAsync("/api/conversions", payload);

        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        _ = problem!.Errors.Should().ContainKey("Resolution");
    }

    [Fact]
    public async Task Post_with_too_high_resolution_returns_validation_error()
    {
        var factory = new WebApplicationFactory<Web.Program>().WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ConnectionStrings:Default", $"Data Source={CreateTempDbPath()}");
            _ = builder.ConfigureServices(services =>
            {
                var proberDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaProber));
                if (proberDescriptor != null)
                {
                    _ = services.Remove(proberDescriptor);
                }
                _ = services.AddSingleton<FakeMediaProber>();
                _ = services.AddSingleton<IMediaProber>(sp => sp.GetRequiredService<FakeMediaProber>());
            });
        });

        var client = factory.CreateClient();
        var fakeProber = factory.Services.GetRequiredService<FakeMediaProber>();
        fakeProber.Streams =
        [
            new MediaStream { Index = 0, CodecType = "video", LanguageTag = null, Title = null },
        ];
        // Simulate 1080p source
        fakeProber.VideoWidth = 1920;
        fakeProber.VideoHeight = 1080;

        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllTextAsync(tempInput, "dummy");
        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir, resolution = 2160 };
        var response = await client.PostAsJsonAsync("/api/conversions", payload);

        // Since FakeMediaProber currently doesn't provide height, API will say unable to determine or allow; instead use real dryrun factory for height-dependent test below
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_with_subtitle_language_uses_external_srt_when_missing_in_streams()
    {
        var factory = new WebApplicationFactory<Web.Program>().WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ConnectionStrings:Default", $"Data Source={CreateTempDbPath()}");
            _ = builder.UseSetting("Ffmpeg:DryRun", "true");
            _ = builder.ConfigureServices(services =>
            {
                var proberDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaProber));
                if (proberDescriptor != null)
                {
                    _ = services.Remove(proberDescriptor);
                }
                _ = services.AddSingleton<FakeMediaProber>();
                _ = services.AddSingleton<IMediaProber>(sp => sp.GetRequiredService<FakeMediaProber>());
            });
        });

        var client = factory.CreateClient();

        var fakeProber = factory.Services.GetRequiredService<FakeMediaProber>();
        fakeProber.Streams =
        [
            new MediaStream { Index = 0, CodecType = "video", LanguageTag = null, Title = null },
            new MediaStream { Index = 1, CodecType = "audio", LanguageTag = "eng", Title = "English" },
        ];

        var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;
        var tempInput = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + ".mkv");
        await File.WriteAllTextAsync(tempInput, "dummy");

        // Create external Swedish subtitle: <video>.<tag>.srt
        var baseName = Path.GetFileNameWithoutExtension(tempInput);
        var extSub = Path.Combine(tempDir, baseName + ".swe.srt");
        await File.WriteAllTextAsync(extSub, "1\n00:00:00,000 --> 00:00:01,000\nHej\n");

        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir, subtitleLanguages = new[] { "Swedish" } };

        var create = await client.PostAsJsonAsync("/api/conversions", payload);
        _ = create.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<ConversionDto>>("/api/conversions");
        var created = list!.Single(x => x.InputPath == payload.inputPath);

        var startResp = await client.PostAsync($"/api/conversions/{created.Id}/start", content: null);
        _ = startResp.EnsureSuccessStatusCode();
        var startPayload = await startResp.Content.ReadFromJsonAsync<RealRunnerTests.StartResponse>();

        // Should include -i <extSub> and map 1:s:0
        _ = startPayload!.Command.Should().Contain($"-i {extSub}");
        _ = startPayload!.Command.Should().Contain("-map 1:s:0");
        _ = startPayload!.Command.Should().Contain("-c:s copy");
    }

    [Fact]
    public async Task Post_with_nonexistent_audio_language_returns_validation_error()
    {
        var factory = new WebApplicationFactory<Web.Program>().WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ConnectionStrings:Default", $"Data Source={CreateTempDbPath()}");
            _ = builder.ConfigureServices(services =>
            {
                var proberDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaProber));
                if (proberDescriptor != null)
                {
                    _ = services.Remove(proberDescriptor);
                }
                _ = services.AddSingleton<FakeMediaProber>();
                _ = services.AddSingleton<IMediaProber>(sp => sp.GetRequiredService<FakeMediaProber>());
            });
        });

        var client = factory.CreateClient();

        var fakeProber = factory.Services.GetRequiredService<FakeMediaProber>();
        fakeProber.Streams =
        [
            new MediaStream { Index = 1, CodecType = "audio", LanguageTag = "eng", Title = "English" }
        ];

        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllTextAsync(tempInput, "dummy");
        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir, audioLanguages = new[] { "Swedish" } };
        var response = await client.PostAsJsonAsync("/api/conversions", payload);

        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        _ = problem!.Errors.Should().ContainKey("AudioLanguages");
    }

    [Fact]
    public async Task Post_with_nonexistent_subtitle_language_returns_validation_error()
    {
        var factory = new WebApplicationFactory<Web.Program>().WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ConnectionStrings:Default", $"Data Source={CreateTempDbPath()}");
            _ = builder.ConfigureServices(services =>
            {
                var proberDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaProber));
                if (proberDescriptor != null)
                {
                    _ = services.Remove(proberDescriptor);
                }
                _ = services.AddSingleton<FakeMediaProber>();
                _ = services.AddSingleton<IMediaProber>(sp => sp.GetRequiredService<FakeMediaProber>());
            });
        });

        var client = factory.CreateClient();

        var fakeProber = factory.Services.GetRequiredService<FakeMediaProber>();
        fakeProber.Streams =
        [
            new MediaStream { Index = 0, CodecType = "video", LanguageTag = null, Title = null },
            new MediaStream { Index = 1, CodecType = "audio", LanguageTag = "eng", Title = "English" }
        ];

        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mkv");
        await File.WriteAllTextAsync(tempInput, "dummy");
        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir, subtitleLanguages = new[] { "Swedish" } };
        var response = await client.PostAsJsonAsync("/api/conversions", payload);

        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        _ = problem!.Errors.Should().ContainKey("SubtitleLanguages");
    }

    [Fact]
    public async Task Get_returns_all_rows()
    {
        var client = _factory.CreateClient();

        var in1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        var in2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllTextAsync(in1, "1");
        await File.WriteAllTextAsync(in2, "2");
        var out1 = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;
        var out2 = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;
        var p1 = new { inputPath = in1, outputDirectory = out1 };
        var p2 = new { inputPath = in2, outputDirectory = out2 };

        var r1 = await client.PostAsJsonAsync("/api/conversions", p1);
        _ = r1.EnsureSuccessStatusCode();
        var r2 = await client.PostAsJsonAsync("/api/conversions", p2);
        _ = r2.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<ConversionDto>>("/api/conversions");
        _ = list.Should().NotBeNull();
        _ = list!.Count.Should().BeGreaterOrEqualTo(2);

        _ = list.Should().Contain(x => x.InputPath == p1.inputPath && x.OutputDirectory == p1.outputDirectory);
        _ = list.Should().Contain(x => x.InputPath == p2.inputPath && x.OutputDirectory == p2.outputDirectory);
    }

    public record ConversionDto(int Id, string InputPath, string OutputDirectory, DateTime CreatedAtUtc, int? Crf, int? AudioBitrateKbps, int? Resolution);
}

public class RealRunnerTests
{
    private static string CreateTempDbPath()
    {
        return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) + ".db";
    }

    [Fact]
    public async Task Start_uses_configured_crf_and_bitrate_in_ffmpeg_command()
    {
        var factory = new WebApplicationFactory<Web.Program>().WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ConnectionStrings:Default", $"Data Source={CreateTempDbPath()}");
            _ = builder.UseSetting("Ffmpeg:DryRun", "true");
            _ = builder.ConfigureServices(services =>
            {
                var proberDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaProber));
                if (proberDescriptor != null)
                {
                    _ = services.Remove(proberDescriptor);
                }
                _ = services.AddSingleton<FakeMediaProber>();
                _ = services.AddSingleton<IMediaProber>(sp => sp.GetRequiredService<FakeMediaProber>());
            });
        });

        var client = factory.CreateClient();

        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllTextAsync(tempInput, "dummy");
        var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;

        var payload = new { inputPath = tempInput, outputDirectory = tempOutDir, crf = 20, audioBitrateKbps = 256, resolution = 720 };

        var create = await client.PostAsJsonAsync("/api/conversions", payload);
        _ = create.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<ApiTests.ConversionDto>>("/api/conversions");
        var item = list!.Single(x => x.InputPath == payload.inputPath);

        var startResp = await client.PostAsync($"/api/conversions/{item.Id}/start", content: null);
        _ = startResp.EnsureSuccessStatusCode();

        var startPayload = await startResp.Content.ReadFromJsonAsync<StartResponse>();
        _ = startPayload.Should().NotBeNull();
        _ = startPayload!.Command.Should().Contain("-crf 20");
        _ = startPayload!.Command.Should().Contain("-b:a 256k");
        _ = startPayload!.Command.Should().Contain("-vf scale=w=1280:h=720:force_original_aspect_ratio=decrease:flags=lanczos");
    }

    [Fact]
    public async Task Start_uses_1080p_and_2160p_resolution_in_ffmpeg_command()
    {
        var factory = new WebApplicationFactory<Web.Program>().WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ConnectionStrings:Default", $"Data Source={CreateTempDbPath()}");
            _ = builder.UseSetting("Ffmpeg:DryRun", "true");
            _ = builder.ConfigureServices(services =>
            {
                var proberDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaProber));
                if (proberDescriptor != null)
                {
                    _ = services.Remove(proberDescriptor);
                }
                _ = services.AddSingleton<FakeMediaProber>();
                _ = services.AddSingleton<IMediaProber>(sp => sp.GetRequiredService<FakeMediaProber>());
            });
        });

        var client = factory.CreateClient();

        async Task AssertResolution(int res, string expected)
        {
            var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
            await File.WriteAllTextAsync(tempInput, "dummy");
            var tempOutDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vc_out_" + Guid.NewGuid().ToString("N"))).FullName;
            var create = await client.PostAsJsonAsync("/api/conversions", new { inputPath = tempInput, outputDirectory = tempOutDir, resolution = res });
            _ = create.EnsureSuccessStatusCode();
            var list = await client.GetFromJsonAsync<List<ApiTests.ConversionDto>>("/api/conversions");
            var item = list!.Single(x => x.InputPath == tempInput);
            var startResp = await client.PostAsync($"/api/conversions/{item.Id}/start", content: null);
            _ = startResp.EnsureSuccessStatusCode();
            var startPayload = await startResp.Content.ReadFromJsonAsync<StartResponse>();
            _ = startPayload!.Command.Should().Contain(expected);
        }

        await AssertResolution(1080, "-vf scale=w=1920:h=1080:force_original_aspect_ratio=decrease:flags=lanczos");
        await AssertResolution(2160, "-vf scale=w=3840:h=2160:force_original_aspect_ratio=decrease:flags=lanczos");
    }

    public sealed class StartResponse
    {
        public string Message { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string Command { get; set; } = string.Empty;
    }
}
