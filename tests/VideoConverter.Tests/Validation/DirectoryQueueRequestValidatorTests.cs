using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using VideoConverter.Common.Models;
using VideoConverter.Common.Validation;
using VideoConverter.Common.Validation.Requests;

namespace VideoConverter.Tests.Validation;

public class DirectoryQueueRequestValidatorTests
{
    private readonly StubFileSystemValidator _fileSystemValidator = new();
    private readonly RequestPropertyValidator _propertyValidator = new();
    private readonly DirectoryQueueRequestValidator _validator;

    public DirectoryQueueRequestValidatorTests()
    {
        _validator = new DirectoryQueueRequestValidator(
            NullLogger<DirectoryQueueRequestValidator>.Instance,
            _fileSystemValidator,
            _propertyValidator);
    }

    [Fact]
    public async Task ValidateAsync_WhenDirectoryMissing_ReturnsError()
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: string.Empty,
            OutputDirectory: "/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: null);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.DirectoryPath));
    }

    [Fact]
    public async Task ValidateAsync_WhenOutputDirectoryInvalid_ReturnsErrorMessage()
    {
        _fileSystemValidator.DirectoryResultOverride = (false, "output invalid");

        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: null);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.OutputDirectory));
        result.ToDictionary()[nameof(request.OutputDirectory)].Should().Contain("output invalid");
    }

    [Fact]
    public async Task ValidateAsync_WhenCrfInvalid_ReturnsError()
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Crf: -1,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: null);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.Crf));
    }

    [Fact]
    public async Task ValidateAsync_WhenExtensionsContainEmpty_AddsError()
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: new[] { ".mkv", string.Empty });

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.Extensions));
    }

    [Fact]
    public async Task ValidateAsync_WhenValid_ReturnsValid()
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            AudioLanguages: new[] { "en" },
            SubtitleLanguages: new[] { "sv" },
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: new[] { ".mkv", ".mp4" },
            VmafEnabled: true,
            RequiredVmaf: 92.5);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.ToDictionary().Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WhenRequiredVmafOutOfRange_ReturnsError()
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: null,
            RequiredVmaf: 101);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(100.0)]
    public async Task ValidateAsync_WhenRequiredVmafAtBoundaries_ReturnsValid(double requiredVmaf)
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: null,
            VmafEnabled: true,
            RequiredVmaf: requiredVmaf);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public async Task ValidateAsync_WhenRequiredVmafOutsideBoundaries_ReturnsError(double requiredVmaf)
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: null,
            RequiredVmaf: requiredVmaf);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
    }

    [Fact]
    public async Task ValidateAsync_WhenVmafDisabledAndRequiredVmafProvided_ReturnsError()
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: null,
            VmafEnabled: false,
            RequiredVmaf: 92.0);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
    }

    [Fact]
    public async Task ValidateAsync_WhenVmafUnspecifiedAndRequiredVmafProvided_ReturnsError()
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/input",
            OutputDirectory: "/output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: null,
            VmafEnabled: null,
            RequiredVmaf: 92.0);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
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
