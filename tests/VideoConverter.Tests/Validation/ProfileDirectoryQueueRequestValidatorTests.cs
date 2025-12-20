namespace VideoConverter.Tests.Validation;

public class ProfileDirectoryQueueRequestValidatorTests
{
    private readonly StubFileSystemValidator _fileSystemValidator = new();
    private readonly RequestPropertyValidator _propertyValidator = new();
    private readonly ProfileDirectoryQueueRequestValidator _validator;

    public ProfileDirectoryQueueRequestValidatorTests()
    {
        _validator = new ProfileDirectoryQueueRequestValidator(
            NullLogger<ProfileDirectoryQueueRequestValidator>.Instance,
            _fileSystemValidator,
            _propertyValidator);
    }

    [Fact]
    public async Task ValidateAsync_WhenDirectoryMissing_ReturnsError()
    {
        var request = new ProfileDirectoryQueueRequestDto(
            DirectoryPath: string.Empty,
            OutputDirectory: "/output",
            Extensions: null,
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.DirectoryPath));
    }

    [Fact]
    public async Task ValidateAsync_WhenOutputDirectoryInvalid_ReturnsErrorMessage()
    {
        _fileSystemValidator.DirectoryResultOverride = (false, "output invalid");

        var request = new ProfileDirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Extensions: null,
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.OutputDirectory));
        result.ToDictionary()[nameof(request.OutputDirectory)].Should().Contain("output invalid");
    }

    [Fact]
    public async Task ValidateAsync_WhenCrfInvalid_ReturnsError()
    {
        var request = new ProfileDirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Extensions: null,
            Crf: -1,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.Crf));
    }

    [Fact]
    public async Task ValidateAsync_WhenExtensionsContainEmpty_AddsError()
    {
        var request = new ProfileDirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Extensions: new[] { ".mkv", string.Empty },
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.Extensions));
    }

    [Fact]
    public async Task ValidateAsync_WhenValid_ReturnsValid()
    {
        var request = new ProfileDirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Extensions: new[] { ".mkv", ".mp4" },
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            AudioLanguages: new[] { "en" },
            SubtitleLanguages: new[] { "sv" },
            OutputFilenameTemplate: null,
            OutputPathTemplate: null);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.ToDictionary().Should().BeEmpty();
    }

    private sealed class StubFileSystemValidator : IFileSystemValidator
    {
        public (bool IsValid, string? Error)? DirectoryResultOverride { get; set; }

        public bool IsValidPath(string? path, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "Path is required.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool IsValidFile(string? filePath, out string? errorMessage)
        {
            return IsValidPath(filePath, out errorMessage);
        }

        public bool IsValidDirectory(string? directoryPath, out string? errorMessage)
        {
            if (DirectoryResultOverride is { } result)
            {
                errorMessage = result.Error;
                return result.IsValid;
            }

            return IsValidPath(directoryPath, out errorMessage);
        }
    }
}
