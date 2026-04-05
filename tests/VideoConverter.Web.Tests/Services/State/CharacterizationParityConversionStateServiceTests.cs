using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Common.Models;
using VideoConverter.Web.Features.ManualResolution.Logic;
using VideoConverter.Web.Services;
using VideoConverter.Web.Services.ApiClient;

namespace VideoConverter.Web.Tests.Services;

public sealed class CharacterizationParityConversionStateServiceTests
{
    [Fact]
    [Trait("Category", "Characterization")]
    public async Task QueueStartPauseStop_StateTransitions_AreStable()
    {
        var api = new Mock<IVideoConverterApiClient>(MockBehavior.Strict);
        var pending = CreateConversion(id: 1, status: ConversionStatus.Pending);
        var running = CreateConversion(id: 1, status: ConversionStatus.Running);
        var failed = CreateConversion(id: 1, status: ConversionStatus.Failed);

        _ = api.SetupSequence(x => x.GetConversionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([pending])
            .ReturnsAsync([running])
            .ReturnsAsync([failed]);
        _ = api.Setup(x => x.StartQueueAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _ = api.Setup(x => x.PauseQueueAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _ = api.Setup(x => x.StopQueueAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        using var sut = new ConversionStateService(api.Object, NullLogger<ConversionStateService>.Instance);
        await sut.LoadConversionsAsync();
        _ = sut.HasPendingConversions.Should().BeTrue();
        _ = sut.IsRunning.Should().BeFalse();
        _ = sut.IsPaused.Should().BeFalse();

        await sut.StartQueueAsync();
        _ = sut.IsRunning.Should().BeTrue();
        _ = sut.IsPaused.Should().BeFalse();

        await sut.PauseQueueAsync();
        _ = sut.IsPaused.Should().BeTrue();

        await sut.StopQueueAsync();
        _ = sut.IsRunning.Should().BeFalse();
        _ = sut.IsPaused.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task QueueStart_WhenBadRequestRace_SetsRunningAndRefreshes()
    {
        var api = new Mock<IVideoConverterApiClient>(MockBehavior.Strict);
        var pending = CreateConversion(id: 9, status: ConversionStatus.Pending);
        var running = CreateConversion(id: 9, status: ConversionStatus.Running);

        _ = api.SetupSequence(x => x.GetConversionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([pending])
            .ReturnsAsync([running]);
        _ = api.Setup(x => x.StartQueueAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("400 Bad Request"));

        using var sut = new ConversionStateService(api.Object, NullLogger<ConversionStateService>.Instance);
        await sut.LoadConversionsAsync();

        await sut.StartQueueAsync();

        _ = sut.IsRunning.Should().BeTrue();
        _ = sut.IsStarting.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task QueueStart_WhenBadRequestStatusCodeRace_SetsRunningAndRefreshes()
    {
        var api = new Mock<IVideoConverterApiClient>(MockBehavior.Strict);
        var pending = CreateConversion(id: 10, status: ConversionStatus.Pending);
        var running = CreateConversion(id: 10, status: ConversionStatus.Running);

        _ = api.SetupSequence(x => x.GetConversionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([pending])
            .ReturnsAsync([running]);
        _ = api.Setup(x => x.StartQueueAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Queue start rejected", inner: null, System.Net.HttpStatusCode.BadRequest));

        using var sut = new ConversionStateService(api.Object, NullLogger<ConversionStateService>.Instance);
        await sut.LoadConversionsAsync();

        await sut.StartQueueAsync();

        _ = sut.IsRunning.Should().BeTrue();
        _ = sut.IsStarting.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task LoadConversions_Then_RemoveConversion_UpdatesQueueFlags()
    {
        var api = new Mock<IVideoConverterApiClient>(MockBehavior.Strict);
        _ = api.Setup(x => x.GetConversionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateConversion(id: 1, status: ConversionStatus.Pending),
                CreateConversion(id: 2, status: ConversionStatus.Completed),
            ]);

        using var sut = new ConversionStateService(api.Object, NullLogger<ConversionStateService>.Instance);
        await sut.LoadConversionsAsync();
        _ = sut.HasPendingConversions.Should().BeTrue();

        sut.RemoveConversion(1);

        _ = sut.HasPendingConversions.Should().BeFalse();
        _ = sut.PendingConversions.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public async Task LoadConversions_WhenApiFails_ThrowsWrappedInvalidOperation()
    {
        var api = new Mock<IVideoConverterApiClient>(MockBehavior.Strict);
        _ = api.Setup(x => x.GetConversionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));

        using var sut = new ConversionStateService(api.Object, NullLogger<ConversionStateService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.LoadConversionsAsync());

        _ = ex.Message.Should().Be("Failed to load conversions");
        _ = ex.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    [Trait("Category", "Characterization")]
    public void ResolveMetadataDialog_StateTransitions_StayDeterministic()
    {
        var defaultSelection = ResolveMetadataDialogLogic.GetDefaultMediaTypeSelection("TvShow");
        _ = defaultSelection.Should().BeNull();

        var request = ResolveMetadataDialogLogic.CreateSearchRequest("Matrix", 1999, MediaType.Movie);
        _ = request.Type.Should().Be(MediaType.Movie);
        _ = request.Query.Should().Be("Matrix");
        _ = request.Year.Should().Be(1999);

        var clearForMovie = ResolveMetadataDialogLogic.ShouldClearEpisodeGroupState(MediaType.Movie);
        var clearForTv = ResolveMetadataDialogLogic.ShouldClearEpisodeGroupState(MediaType.TvShow);
        var fetchForTv = ResolveMetadataDialogLogic.ShouldFetchEpisodeGroups(MediaType.TvShow);
        var fetchForMovie = ResolveMetadataDialogLogic.ShouldFetchEpisodeGroups(MediaType.Movie);

        _ = clearForMovie.Should().BeTrue();
        _ = clearForTv.Should().BeTrue();
        _ = fetchForTv.Should().BeTrue();
        _ = fetchForMovie.Should().BeFalse();
    }

    private static ConversionDto CreateConversion(int id, ConversionStatus status)
    {
        return new ConversionDto(
            Id: id,
            InputPath: $"input-{id}.mkv",
            OutputDirectory: "out",
            CreatedAtUtc: DateTime.UtcNow,
            Status: status,
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 720,
            MetadataBasedFilename: null,
            DeletedAtUtc: null,
            TmdbId: null,
            MatchedTitle: null,
            MatchedYear: null,
            MatchedSeason: null,
            MatchedEpisode: null,
            SelectedAudioStreamIndicesCsv: null,
            SelectedSubtitleStreamIndicesCsv: null,
            SelectedExternalSubtitlePathsCsv: null,
            DetectedCrop: null,
            DetectedGrainLevel: GrainLevel.Low,
            OutputPathTemplate: null);
    }
}
