using FluentAssertions;
using VideoConverter.Web.Features.Conversions.Services;

namespace VideoConverter.Tests.Services.Conversion;

public class ConversionOutputPlannerTests
{
    [Fact]
    public void Plan_UsesMetadataBasedFilename_WhenPresent()
    {
        var planner = new ConversionOutputPlanner();
        var temp = Path.GetTempPath();
        var conversion = new VideoConverter.Web.Shared.Domain.Entities.Conversion
        {
            InputPath = Path.Combine(temp, "in.mp4"),
            OutputDirectory = temp,
            MetadataBasedFilename = "Show.S01E01.Pilot.mkv",
        };

        var (outputFile, title) = planner.Plan(conversion);
        _ = outputFile.Should().EndWith("Show.S01E01.Pilot.mkv");
        _ = title.Should().Be("in");
    }

    [Fact]
    public void Plan_FallsBackToInputName_WhenNoMetadataFilename()
    {
        var planner = new ConversionOutputPlanner();
        var temp = Path.GetTempPath();
        var conversion = new VideoConverter.Web.Shared.Domain.Entities.Conversion
        {
            InputPath = Path.Combine(temp, "movie.mp4"),
            OutputDirectory = temp,
        };

        var (outputFile, title) = planner.Plan(conversion);
        _ = outputFile.Should().EndWith("movie.mkv");
        _ = title.Should().Be("movie");
    }
}
