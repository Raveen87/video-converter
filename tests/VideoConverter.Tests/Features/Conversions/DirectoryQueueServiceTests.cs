using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Tests.TestUtils;
using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Conversions.Validation;
using VideoConverter.Web.Features.Profiles.Validation;
using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Tests.Features.Conversions;

public class DirectoryQueueServiceTests
{
    [Fact]
    public async Task QueueDirectoryAsync_DefaultExtensions_QueuesMkvAndMp4()
    {
        using var tempRoot = new TempDir();
        var inputDir = tempRoot.CreateSubdir("in");
        var outputDir = tempRoot.CreateSubdir("out");
        var mkv = Path.Combine(inputDir, "a.mkv");
        var mp4 = Path.Combine(inputDir, "b.mp4");
        var avi = Path.Combine(inputDir, "c.avi");
        await File.WriteAllTextAsync(mkv, "x");
        await File.WriteAllTextAsync(mp4, "x");
        await File.WriteAllTextAsync(avi, "x");

        var (db, svc) = CreateSut();
        var req = new DirectoryQueueRequestDto(inputDir, outputDir, 23, 128, AudioChannels: null, Resolution: 1080, AudioLanguages: null, SubtitleLanguages: null, ProfileId: null, OutputFilenameTemplate: null, Extensions: null);

        var result = await svc.QueueDirectoryAsync(req, CancellationToken.None);

        _ = result.TotalFilesFound.Should().Be(2);
        _ = result.Queued.Should().Be(2);
        _ = result.Invalid.Should().Be(0);
        _ = (await db.Conversions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task QueueDirectoryAsync_ExtensionsOverride_IncludesAvi()
    {
        using var tempRoot = new TempDir();
        var inputDir = tempRoot.CreateSubdir("in");
        var outputDir = tempRoot.CreateSubdir("out");
        var mkv = Path.Combine(inputDir, "a.mkv");
        var avi = Path.Combine(inputDir, "b.avi");
        await File.WriteAllTextAsync(mkv, "x");
        await File.WriteAllTextAsync(avi, "x");

        var (db, svc) = CreateSut();
        var req = new DirectoryQueueRequestDto(inputDir, outputDir, 23, 128, AudioChannels: null, Resolution: 1080, AudioLanguages: null, SubtitleLanguages: null, ProfileId: null, OutputFilenameTemplate: null, Extensions: new[] { ".mkv", ".avi" });

        var result = await svc.QueueDirectoryAsync(req, CancellationToken.None);

        _ = result.TotalFilesFound.Should().Be(2);
        _ = result.Queued.Should().Be(2);
        _ = (await db.Conversions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task QueueDirectoryAsync_SkipsDuplicates_WhenAlreadyInDb()
    {
        using var tempRoot = new TempDir();
        var inputDir = tempRoot.CreateSubdir("in");
        var outputDir = tempRoot.CreateSubdir("out");
        var mkv = Path.Combine(inputDir, "a.mkv");
        var mp4 = Path.Combine(inputDir, "b.mp4");
        await File.WriteAllTextAsync(mkv, "x");
        await File.WriteAllTextAsync(mp4, "x");

        var (db, svc) = CreateSut();
        db.Conversions.Add(new Conversion { InputPath = mkv, OutputDirectory = outputDir, CreatedAtUtc = DateTime.UtcNow });
        _ = await db.SaveChangesAsync();

        var req = new DirectoryQueueRequestDto(inputDir, outputDir, null, null, AudioChannels: null, Resolution: null, AudioLanguages: null, SubtitleLanguages: null, ProfileId: null, OutputFilenameTemplate: null, Extensions: null);
        var result = await svc.QueueDirectoryAsync(req, CancellationToken.None);

        _ = result.TotalFilesFound.Should().Be(2);
        _ = result.SkippedDuplicates.Should().Be(1);
        _ = result.Queued.Should().Be(1);
        _ = (await db.Conversions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task QueueDirectoryAsync_ProfileOverrides_OutputDirectory()
    {
        using var tempRoot = new TempDir();
        var inputDir = tempRoot.CreateSubdir("in");
        var outputDir = tempRoot.CreateSubdir("out");
        var mkv = Path.Combine(inputDir, "a.mkv");
        await File.WriteAllTextAsync(mkv, "x");

        var (db, svc) = CreateSut();
        var profile = new Profile { OutputDirectory = Path.Combine(tempRoot.Path, "profileOut") };
        db.Profiles.Add(profile);
        _ = await db.SaveChangesAsync();

        Directory.CreateDirectory(profile.OutputDirectory!);

        var req = new DirectoryQueueRequestDto(inputDir, outputDir, null, null, AudioChannels: null, Resolution: null, AudioLanguages: null, SubtitleLanguages: null, ProfileId: profile.Id, OutputFilenameTemplate: null, Extensions: null);
        var result = await svc.QueueDirectoryAsync(req, CancellationToken.None);

        _ = result.Queued.Should().Be(1);
        var saved = await db.Conversions.AsNoTracking().FirstAsync();
        _ = saved.OutputDirectory.Should().Be(profile.OutputDirectory);
    }

    private static (AppDbContext Db, IDirectoryQueueService Svc) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        var conversionService = new StubConversionService();
        var requestPropertyValidator = new RequestPropertyValidator();
        var fileSystemValidator = new FileSystemValidator();
        var conversionValidator = new ConversionCreateValidator(NullLogger<ConversionCreateValidator>.Instance, fileSystemValidator, requestPropertyValidator);
        var profileExistsValidator = new ProfileExistsValidator(NullLogger<ProfileExistsValidator>.Instance);
        var effectiveBuilder = new EffectiveConversionRequestBuilder(NullLogger<EffectiveConversionRequestBuilder>.Instance);
        var svc = new DirectoryQueueService(db, conversionService, conversionValidator, profileExistsValidator, effectiveBuilder, fileSystemValidator, NullLogger<DirectoryQueueService>.Instance);
        return (db, svc);
    }
}
