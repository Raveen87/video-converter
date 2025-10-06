using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

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
      });

    [Fact]
    public async Task Post_creates_row_in_sqlite()
    {
        var client = _factory.CreateClient();

        var payload = new { inputPath = "C:/videos/input.mp4", outputDirectory = "C:/videos/out" };

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

        var p1 = new { inputPath = "in1.mp4", outputDirectory = "out1" };
        var p2 = new { inputPath = "in2.mp4", outputDirectory = "out2" };

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
