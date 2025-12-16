using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Conversions.Validation;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;
using VideoConverter.Tests.Fakes;
using VideoConverter.Tests.TestUtils;

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
        var req = new DirectoryQueueRequestDto(inputDir, outputDir, 23, 128, AudioChannels: null, Resolution: 1080, AudioLanguages: null, SubtitleLanguages: null, OutputFilenameTemplate: null, OutputPathTemplate: null, Extensions: null);

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
        var req = new DirectoryQueueRequestDto(inputDir, outputDir, 23, 128, AudioChannels: null, Resolution: 1080, AudioLanguages: null, SubtitleLanguages: null, OutputFilenameTemplate: null, OutputPathTemplate: null, Extensions: new[] { ".mkv", ".avi" });

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

        var req = new DirectoryQueueRequestDto(inputDir, outputDir, null, null, AudioChannels: null, Resolution: null, AudioLanguages: null, SubtitleLanguages: null, OutputFilenameTemplate: null, OutputPathTemplate: null, Extensions: null);
        var result = await svc.QueueDirectoryAsync(req, CancellationToken.None);

        _ = result.TotalFilesFound.Should().Be(2);
        _ = result.SkippedDuplicates.Should().Be(1);
        _ = result.Queued.Should().Be(1);
        _ = (await db.Conversions.CountAsync()).Should().Be(2);
    }

    private static (AppDbContext Db, IDirectoryQueueService Svc) CreateSut()
    {
        var db = DbContextTestFactory.CreateInMemory();
        var conversionService = new StubConversionService();
        var requestPropertyValidator = new RequestPropertyValidator();
        var fileSystemValidator = new FileSystemValidator();
        var conversionValidator = new ConversionCreateValidator(NullLogger<ConversionCreateValidator>.Instance, fileSystemValidator, requestPropertyValidator);
        var notificationService = new FakeNotificationService();
        var svc = new DirectoryQueueService(db, conversionService, conversionValidator, fileSystemValidator, notificationService, NullLogger<DirectoryQueueService>.Instance);
        return (db, svc);
    }
}
