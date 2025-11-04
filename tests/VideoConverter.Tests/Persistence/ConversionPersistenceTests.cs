using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Infrastructure.Persistence;

namespace VideoConverter.Tests.Persistence;

public class ConversionPersistenceTests
{
    [Fact]
    public async Task AppDbContext_CanCreateAndRetrieveConversion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AppDbContext(options);
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Crf = 23,
            AudioBitrateKbps = 128,
            Resolution = 1080,
            SelectedAudioStreamIndicesCsv = "0,1",
            SelectedSubtitleStreamIndicesCsv = "2",
            SelectedExternalSubtitlePathsCsv = "sub.srt",
            DetectedCrop = "1920:1080:0:0",
        };
        _ = context.Conversions.Add(conversion);
        _ = await context.SaveChangesAsync();
        var retrieved = await context.Conversions.FirstAsync();
        _ = retrieved.InputPath.Should().Be("test.mp4");
        _ = retrieved.OutputDirectory.Should().Be("output");
        _ = retrieved.Crf.Should().Be(23);
        _ = retrieved.AudioBitrateKbps.Should().Be(128);
        _ = retrieved.Resolution.Should().Be(1080);
        _ = retrieved.SelectedAudioStreamIndicesCsv.Should().Be("0,1");
        _ = retrieved.SelectedSubtitleStreamIndicesCsv.Should().Be("2");
        _ = retrieved.SelectedExternalSubtitlePathsCsv.Should().Be("sub.srt");
        _ = retrieved.DetectedCrop.Should().Be("1920:1080:0:0");
    }

    [Fact]
    public async Task AppDbContext_CanQueryConversionsByDateRange()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AppDbContext(options);
        var baseDate = DateTime.UtcNow;
        var conversions = new[]
        {
            new Conversion { InputPath = "old.mp4", OutputDirectory = "out", CreatedAtUtc = baseDate.AddDays(-2) },
            new Conversion { InputPath = "recent.mp4", OutputDirectory = "out", CreatedAtUtc = baseDate.AddDays(-1) },
            new Conversion { InputPath = "new.mp4", OutputDirectory = "out", CreatedAtUtc = baseDate },
        };
        context.Conversions.AddRange(conversions);
        _ = await context.SaveChangesAsync();
        var recentConversions = await context.Conversions
            .Where(c => c.CreatedAtUtc >= baseDate.AddDays(-1))
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();
        _ = recentConversions.Should().HaveCount(2);
        _ = recentConversions[0].InputPath.Should().Be("new.mp4");
        _ = recentConversions[1].InputPath.Should().Be("recent.mp4");
    }

    [Fact]
    public async Task AppDbContext_CanQueryConversionsByResolution()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AppDbContext(options);
        var conversions = new[]
        {
            new Conversion { InputPath = "720p.mp4", OutputDirectory = "out", Resolution = 720 },
            new Conversion { InputPath = "1080p.mp4", OutputDirectory = "out", Resolution = 1080 },
            new Conversion { InputPath = "2160p.mp4", OutputDirectory = "out", Resolution = 2160 },
            new Conversion { InputPath = "nores.mp4", OutputDirectory = "out", Resolution = null },
        };
        context.Conversions.AddRange(conversions);
        _ = await context.SaveChangesAsync();
        var hdConversions = await context.Conversions.Where(c => c.Resolution == 1080).ToListAsync();
        var allResolutions = await context.Conversions.Where(c => c.Resolution != null).Select(c => c.Resolution!.Value).Distinct().OrderBy(r => r).ToListAsync();
        _ = hdConversions.Should().HaveCount(1);
        _ = hdConversions[0].InputPath.Should().Be("1080p.mp4");
        _ = allResolutions.Should().BeEquivalentTo([720, 1080, 2160]);
    }

    [Fact]
    public async Task AppDbContext_CanHandleNullOptionalFields()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AppDbContext(options);
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Crf = null,
            AudioBitrateKbps = null,
            Resolution = null,
            SelectedAudioStreamIndicesCsv = null,
            SelectedSubtitleStreamIndicesCsv = null,
            SelectedExternalSubtitlePathsCsv = null,
            DetectedCrop = null,
        };
        _ = context.Conversions.Add(conversion);
        _ = await context.SaveChangesAsync();
        var retrieved = await context.Conversions.FirstAsync();
        _ = retrieved.Crf.Should().BeNull();
        _ = retrieved.AudioBitrateKbps.Should().BeNull();
        _ = retrieved.Resolution.Should().BeNull();
        _ = retrieved.SelectedAudioStreamIndicesCsv.Should().BeNull();
        _ = retrieved.SelectedSubtitleStreamIndicesCsv.Should().BeNull();
        _ = retrieved.SelectedExternalSubtitlePathsCsv.Should().BeNull();
        _ = retrieved.DetectedCrop.Should().BeNull();
    }
}
