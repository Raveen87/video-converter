using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Features.Metadata.Models;
using VideoConverter.Web.Features.Metadata.Services;
using VideoConverter.Web.Shared.Infrastructure.Persistence;

namespace VideoConverter.Tests.Services.Conversion;

public class MetadataFetchFailureBehaviorTests
{
    [Fact]
    public async Task DefaultToInputName_UsesTemplateWithParsedValues()
    {
        var mockMetadata = new Mock<IMetadataService>();
        _ = mockMetadata.Setup(x => x.GetMovieMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MovieMetadata?)null);

        var mockParser = new Mock<IFilenameParser>();
        _ = mockParser.Setup(x => x.ParseFilename("Unknown.Movie.2020.mp4"))
            .Returns(new ParsedFilename("Unknown.Movie.2020.mp4", MediaType.Movie, "Unknown Movie", 2020));

        var mockTemplate = new Mock<IFilenameTemplateEngine>();
        _ = mockTemplate.Setup(x => x.Render(It.IsAny<MediaMetadata>(), ".mp4", "{Title}.{Year}"))
            .Returns("Unknown Movie.2020");

        var mockGenerator = new Mock<IFilenameGenerator>();

        var behavior = Options.Create(new MetadataFetchOptions { OnFailureBehavior = FetchFailureBehavior.DefaultToInputName });
        var svc = CreateService(mockMetadata.Object, mockParser.Object, mockTemplate.Object, mockGenerator.Object, behavior);

        var dto = new ConversionCreateDto("Unknown.Movie.2020.mp4", "out", null, null, null, null, null, null, "{Title}.{Year}");
        var result = await svc.CreateConversionAsync(dto, CancellationToken.None);

        Assert.Equal("Unknown Movie.2020", result.MetadataBasedFilename);
        mockTemplate.Verify(x => x.Render(It.IsAny<MediaMetadata>(), ".mp4", "{Title}.{Year}"), Times.Once);
    }

    [Fact]
    public async Task QueueForManualResolution_InsertsRowAndThrows()
    {
        var mockMetadata = new Mock<IMetadataService>();
        _ = mockMetadata.Setup(x => x.GetMovieMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MovieMetadata?)null);

        var mockParser = new Mock<IFilenameParser>();
        _ = mockParser.Setup(x => x.ParseFilename("Unknown.Movie.2020.mp4"))
            .Returns(new ParsedFilename("Unknown.Movie.2020.mp4", MediaType.Movie, "Unknown Movie", 2020));

        var mockTemplate = new Mock<IFilenameTemplateEngine>();
        var mockGenerator = new Mock<IFilenameGenerator>();

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        _ = await db.Database.EnsureCreatedAsync();

        var behavior = Options.Create(new MetadataFetchOptions { OnFailureBehavior = FetchFailureBehavior.QueueForManualResolution });
        var svc = CreateService(mockMetadata.Object, mockParser.Object, mockTemplate.Object, mockGenerator.Object, behavior, db);

        var dto = new ConversionCreateDto("Unknown.Movie.2020.mp4", "out", null, null, null, null, null, null, "{Title}.{Year}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateConversionAsync(dto, CancellationToken.None));
        Assert.Equal(1, await db.MetadataResolutionQueue.CountAsync());
    }

    [Fact]
    public async Task SkipItem_DoesNotInsertRowAndThrows()
    {
        var mockMetadata = new Mock<IMetadataService>();
        _ = mockMetadata.Setup(x => x.GetMovieMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MovieMetadata?)null);

        var mockParser = new Mock<IFilenameParser>();
        _ = mockParser.Setup(x => x.ParseFilename("Unknown.Movie.2020.mp4"))
            .Returns(new ParsedFilename("Unknown.Movie.2020.mp4", MediaType.Movie, "Unknown Movie", 2020));

        var mockTemplate = new Mock<IFilenameTemplateEngine>();
        var mockGenerator = new Mock<IFilenameGenerator>();

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        _ = await db.Database.EnsureCreatedAsync();

        var behavior = Options.Create(new MetadataFetchOptions { OnFailureBehavior = FetchFailureBehavior.SkipItem });
        var svc = CreateService(mockMetadata.Object, mockParser.Object, mockTemplate.Object, mockGenerator.Object, behavior, db);

        var dto = new ConversionCreateDto("Unknown.Movie.2020.mp4", "out", null, null, null, null, null, null, "{Title}.{Year}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateConversionAsync(dto, CancellationToken.None));
        Assert.Equal(0, await db.MetadataResolutionQueue.CountAsync());
    }

    private static ConversionService CreateService(
        IMetadataService metadataService,
        IFilenameParser filenameParser,
        IFilenameTemplateEngine templateEngine,
        IFilenameGenerator filenameGenerator,
        IOptions<MetadataFetchOptions> behaviorOptions,
        AppDbContext? db = null)
    {
        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var logger = new Mock<ILogger<ConversionService>>();

        return new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            metadataService,
            filenameParser,
            filenameGenerator,
            templateEngine,
            Options.Create(new FilenameTemplateOptions { DefaultTemplate = null }),
            logger.Object,
            behaviorOptions,
            db);
    }
}
