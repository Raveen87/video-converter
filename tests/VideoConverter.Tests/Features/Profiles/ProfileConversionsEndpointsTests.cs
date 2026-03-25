using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Profiles.Api;
using VideoConverter.Api.Features.Profiles.Validation;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Validation;
using VideoConverter.Common.Validation.Requests;
using VideoConverter.Tests.Fakes;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Features.Profiles;

public sealed class ProfileConversionsEndpointsTests
{
    [Fact]
    public async Task QueueConversionAsync_UsesRequestRequiredVmafOverride()
    {
        using var tempRoot = new TempDir();
        var outputDir = tempRoot.CreateSubdir("out");
        var inputPath = Path.Combine(tempRoot.Path, "video.mkv");
        await File.WriteAllTextAsync(inputPath, "x");

        await using var db = DbContextTestFactory.CreateInMemory();
        var profile = new Profile
        {
            Name = "Profile",
            OutputDirectory = outputDir,
            Crf = 30,
            RequiredVmaf = 90.0,
            VmafEnabled = true,
        };
        _ = db.Profiles.Add(profile);
        _ = await db.SaveChangesAsync();

        var conversionService = new CapturingConversionService();
        var endpoints = CreateEndpoints(db, conversionService, new CapturingDirectoryBatchService());

        var request = new ProfileConversionCreateDto(
            InputPath: inputPath,
            OutputDirectory: outputDir,
            VmafEnabled: true,
            RequiredVmaf: 95.2);

        var result = await endpoints.QueueConversionAsync(profile.Id, request, CancellationToken.None);

        _ = result.Should().NotBeNull();
        _ = conversionService.LastRequest.Should().NotBeNull();
        _ = conversionService.LastRequest!.RequiredVmaf.Should().Be(95.2);
        _ = conversionService.LastRequest.VmafEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task QueueDirectoryAsync_FallsBackToProfileRequiredVmaf_WhenRequestMissing()
    {
        using var tempRoot = new TempDir();
        var inputDir = tempRoot.CreateSubdir("in");
        var outputDir = tempRoot.CreateSubdir("out");

        await using var db = DbContextTestFactory.CreateInMemory();
        var profile = new Profile
        {
            Name = "Profile",
            OutputDirectory = outputDir,
            RequiredVmaf = 91.1,
            VmafEnabled = true,
        };
        _ = db.Profiles.Add(profile);
        _ = await db.SaveChangesAsync();

        var batchService = new CapturingDirectoryBatchService();
        var endpoints = CreateEndpoints(db, new CapturingConversionService(), batchService);

        var request = new ProfileDirectoryQueueRequestDto(
            DirectoryPath: inputDir,
            OutputDirectory: outputDir,
            Extensions: null,
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            VmafEnabled: null,
            RequiredVmaf: null);

        _ = await endpoints.QueueDirectoryAsync(profile.Id, request, CancellationToken.None);

        _ = batchService.LastRequest.Should().NotBeNull();
        _ = batchService.LastRequest!.RequiredVmaf.Should().Be(91.1);
        _ = batchService.LastRequest.VmafEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task QueueConversionAsync_WhenRequestDisablesVmaf_ClearsInheritedRequiredVmaf()
    {
        using var tempRoot = new TempDir();
        var outputDir = tempRoot.CreateSubdir("out");
        var inputPath = Path.Combine(tempRoot.Path, "video.mkv");
        await File.WriteAllTextAsync(inputPath, "x");

        await using var db = DbContextTestFactory.CreateInMemory();
        var profile = new Profile
        {
            Name = "Profile",
            OutputDirectory = outputDir,
            RequiredVmaf = 90.0,
            VmafEnabled = true,
        };
        _ = db.Profiles.Add(profile);
        _ = await db.SaveChangesAsync();

        var conversionService = new CapturingConversionService();
        var endpoints = CreateEndpoints(db, conversionService, new CapturingDirectoryBatchService());

        var request = new ProfileConversionCreateDto(
            InputPath: inputPath,
            OutputDirectory: outputDir,
            VmafEnabled: false,
            RequiredVmaf: null);

        var result = await endpoints.QueueConversionAsync(profile.Id, request, CancellationToken.None);

        _ = result.Should().NotBeNull();
        _ = conversionService.LastRequest.Should().NotBeNull();
        _ = conversionService.LastRequest!.VmafEnabled.Should().BeFalse();
        _ = conversionService.LastRequest.RequiredVmaf.Should().BeNull();
    }

    [Fact]
    public async Task QueueDirectoryAsync_WhenRequestDisablesVmaf_ClearsInheritedRequiredVmaf()
    {
        using var tempRoot = new TempDir();
        var inputDir = tempRoot.CreateSubdir("in");
        var outputDir = tempRoot.CreateSubdir("out");

        await using var db = DbContextTestFactory.CreateInMemory();
        var profile = new Profile
        {
            Name = "Profile",
            OutputDirectory = outputDir,
            RequiredVmaf = 92.3,
            VmafEnabled = true,
        };
        _ = db.Profiles.Add(profile);
        _ = await db.SaveChangesAsync();

        var batchService = new CapturingDirectoryBatchService();
        var endpoints = CreateEndpoints(db, new CapturingConversionService(), batchService);

        var request = new ProfileDirectoryQueueRequestDto(
            DirectoryPath: inputDir,
            OutputDirectory: outputDir,
            Extensions: null,
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            VmafEnabled: false,
            RequiredVmaf: null);

        _ = await endpoints.QueueDirectoryAsync(profile.Id, request, CancellationToken.None);

        _ = batchService.LastRequest.Should().NotBeNull();
        _ = batchService.LastRequest!.VmafEnabled.Should().BeFalse();
        _ = batchService.LastRequest.RequiredVmaf.Should().BeNull();
    }

    private static ProfileConversionsEndpoints CreateEndpoints(
        AppDbContext db,
        IConversionService conversionService,
        IDirectoryBatchService directoryBatchService)
    {
        var profileExistsValidator = new ProfileExistsValidator(NullLogger<ProfileExistsValidator>.Instance);
        var requestPropertyValidator = new RequestPropertyValidator();
        var fileSystemValidator = new FileSystemValidator();
        var conversionValidator = new ConversionCreateValidator(
            NullLogger<ConversionCreateValidator>.Instance,
            fileSystemValidator,
            requestPropertyValidator);

        return new ProfileConversionsEndpoints(
            db,
            profileExistsValidator,
            new EffectiveConversionRequestBuilder(NullLogger<EffectiveConversionRequestBuilder>.Instance),
            conversionValidator,
            conversionService,
            directoryBatchService,
            new FakeNotificationService(),
            NullLogger<ProfileConversionsEndpoints>.Instance);
    }

    private sealed class CapturingConversionService : IConversionService
    {
        public ConversionCreateDto? LastRequest { get; private set; }

        public Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new Conversion
            {
                InputPath = request.InputPath,
                OutputDirectory = request.OutputDirectory,
                CreatedAtUtc = DateTime.UtcNow,
                Status = ConversionStatus.Pending,
                RequiredVmaf = request.RequiredVmaf,
                VmafEnabled = request.VmafEnabled ?? false,
            });
        }

        public Task<Conversion> CreateConversionWithMetadataAsync(ConversionCreateDto request, global::VideoConverter.Api.Features.Metadata.Models.MediaMetadata metadata, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<LanguageProcessingResult> ProcessLanguagesAsync(string inputPath, IEnumerable<string>? audioLanguages, IEnumerable<string>? subtitleLanguages, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Conversion> UpdateMetadataAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingDirectoryBatchService : IDirectoryBatchService
    {
        public DirectoryQueueRequestDto? LastRequest { get; private set; }

        public Task<int> CreateBatchAsync(DirectoryQueueRequestDto request, int? profileId, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(42);
        }
    }
}
