using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VideoConverter.Web.Services;

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

    public record ConversionDto(int Id, string InputPath, string OutputDirectory, DateTime CreatedAtUtc);
}
