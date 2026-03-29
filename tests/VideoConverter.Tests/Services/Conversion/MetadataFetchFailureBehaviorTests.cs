using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils.Builders;

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

        var dto = new ConversionCreateDtoBuilder()
            .WithInputPath("Unknown.Movie.2020.mp4")
            .WithOutputDirectory("out")
            .Build();
        dto = dto with { OutputFilenameTemplate = "{Title}.{Year}" };
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

        using var dbWrapper = TestUtils.DbContextTestFactory.CreateSqliteInMemory();
        var db = dbWrapper.Context;

        var behavior = Options.Create(new MetadataFetchOptions { OnFailureBehavior = FetchFailureBehavior.QueueForManualResolution });
        var svc = CreateService(mockMetadata.Object, mockParser.Object, mockTemplate.Object, mockGenerator.Object, behavior, db);

        var dto = new ConversionCreateDtoBuilder()
            .WithInputPath("Unknown.Movie.2020.mp4")
            .WithOutputDirectory("out")
            .Build();
        dto = dto with { OutputFilenameTemplate = "{Title}.{Year}" };

        await Assert.ThrowsAsync<ApiErrorException>(() => svc.CreateConversionAsync(dto, CancellationToken.None));
        Assert.Equal(1, await db.ManualResolutionQueue.CountAsync());
    }

    [Fact]
    public async Task QueueForManualResolution_AddsNotification()
    {
        var mockMetadata = new Mock<IMetadataService>();
        _ = mockMetadata.Setup(x => x.GetMovieMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MovieMetadata?)null);

        var mockParser = new Mock<IFilenameParser>();
        _ = mockParser.Setup(x => x.ParseFilename("Unknown.Movie.2020.mp4"))
            .Returns(new ParsedFilename("Unknown.Movie.2020.mp4", MediaType.Movie, "Unknown Movie", 2020));

        var mockTemplate = new Mock<IFilenameTemplateEngine>();
        var mockGenerator = new Mock<IFilenameGenerator>();
        var mockNotifications = new Mock<INotificationService>();

        using var dbWrapper = TestUtils.DbContextTestFactory.CreateSqliteInMemory();
        var db = dbWrapper.Context;

        var behavior = Options.Create(new MetadataFetchOptions { OnFailureBehavior = FetchFailureBehavior.QueueForManualResolution });
        var svc = CreateService(mockMetadata.Object, mockParser.Object, mockTemplate.Object, mockGenerator.Object, behavior, db, mockNotifications.Object);

        var dto = new ConversionCreateDtoBuilder()
            .WithInputPath("Unknown.Movie.2020.mp4")
            .WithOutputDirectory("out")
            .Build();

        await Assert.ThrowsAsync<ApiErrorException>(() => svc.CreateConversionAsync(dto, CancellationToken.None));

        mockNotifications.Verify(
            x => x.AddAsync(
                It.Is<string>(s => s == "Metadata Lookup Failed"),
                It.Is<string>(s => s.Contains("Item added to Metadata Resolution Queue")),
                NotificationType.Warning,
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        using var dbWrapper = TestUtils.DbContextTestFactory.CreateSqliteInMemory();
        var db = dbWrapper.Context;

        var behavior = Options.Create(new MetadataFetchOptions { OnFailureBehavior = FetchFailureBehavior.SkipItem });
        var svc = CreateService(mockMetadata.Object, mockParser.Object, mockTemplate.Object, mockGenerator.Object, behavior, db);

        var dto = new ConversionCreateDtoBuilder()
            .WithInputPath("Unknown.Movie.2020.mp4")
            .WithOutputDirectory("out")
            .Build();
        dto = dto with { OutputFilenameTemplate = "{Title}.{Year}" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateConversionAsync(dto, CancellationToken.None));
        Assert.Equal(0, await db.ManualResolutionQueue.CountAsync());
    }

    private static ConversionService CreateService(
        IMetadataService metadataService,
        IFilenameParser filenameParser,
        IFilenameTemplateEngine templateEngine,
        IFilenameGenerator filenameGenerator,
        IOptions<MetadataFetchOptions> behaviorOptions,
        AppDbContext? db = null,
        INotificationService? notificationService = null)
    {
        var mockProber = new Mock<IMediaProber>();
        _ = mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoConverter.Api.Shared.Domain.ValueObjects.MediaProbeResult
            {
                Streams = [new VideoConverter.Api.Shared.Domain.ValueObjects.MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
            });

        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        _ = mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<VideoConverter.Api.Shared.Domain.ValueObjects.MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns([0]);

        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockGrainDetector = new Mock<IGrainDetector>();
        _ = mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);
        var logger = new Mock<ILogger<ConversionService>>();

        return new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockGrainDetector.Object,
            metadataService,
            filenameParser,
            filenameGenerator,
            templateEngine,
            Options.Create(new FilenameTemplateOptions { DefaultTemplate = null }),
            logger.Object,
            behaviorOptions,
            Options.Create(new StreamSelectionOptions { OnFailureBehavior = StreamSelectionFailureBehavior.Fail }),
            db,
            notificationService);
    }
}
