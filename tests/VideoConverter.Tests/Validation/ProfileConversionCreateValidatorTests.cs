using VideoConverter.Common.Validation.Requests;

namespace VideoConverter.Tests.Validation;

public class ProfileConversionCreateValidatorTests
{
    private readonly StubFileSystemValidator _fileSystemValidator = new();
    private readonly ProfileConversionCreateValidator _validator;

    public ProfileConversionCreateValidatorTests()
    {
        _validator = new ProfileConversionCreateValidator(
            NullLogger<ProfileConversionCreateValidator>.Instance,
            _fileSystemValidator,
            new RequestPropertyValidator());
    }

    [Fact]
    public async Task ValidateAsync_WhenRequiredVmafIsOutOfRange_ReturnsError()
    {
        var request = new ProfileConversionCreateDto("/input/file.mkv", "/output", RequiredVmaf: 101);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(100.0)]
    public async Task ValidateAsync_WhenRequiredVmafAtBoundaries_ReturnsValid(double requiredVmaf)
    {
        var request = new ProfileConversionCreateDto("/input/file.mkv", "/output", VmafEnabled: true, RequiredVmaf: requiredVmaf);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public async Task ValidateAsync_WhenRequiredVmafOutOfRange_ReturnsError(double requiredVmaf)
    {
        var request = new ProfileConversionCreateDto("/input/file.mkv", "/output", RequiredVmaf: requiredVmaf);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
    }

    [Fact]
    public async Task ValidateAsync_WhenVmafDisabledAndRequiredVmafProvided_ReturnsError()
    {
        var request = new ProfileConversionCreateDto("/input/file.mkv", "/output", VmafEnabled: false, RequiredVmaf: 92.0);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
    }

    [Fact]
    public async Task ValidateAsync_WhenVmafUnspecifiedAndRequiredVmafProvided_ReturnsError()
    {
        var request = new ProfileConversionCreateDto("/input/file.mkv", "/output", VmafEnabled: null, RequiredVmaf: 92.0);

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.RequiredVmaf));
    }

    [Fact]
    public async Task ValidateAsync_WhenInputInvalid_ReturnsError()
    {
        _fileSystemValidator.FileResultOverride = (false, "file missing");
        var request = new ProfileConversionCreateDto(string.Empty, "/output");

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ToDictionary().Should().ContainKey(nameof(request.InputPath));
    }

    [Fact]
    public async Task ValidateAsync_WhenValid_ReturnsValid()
    {
        var request = new ProfileConversionCreateDto("/input/file.mkv", "/output");

        var result = await _validator.ValidateAsync(request, CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    private sealed class StubFileSystemValidator : IFileSystemValidator
    {
        public (bool IsValid, string? Error)? FileResultOverride { get; set; }

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
            if (FileResultOverride is { } result)
            {
                errorMessage = result.Error;
                return result.IsValid;
            }

            return IsValidPath(filePath, out errorMessage);
        }

        public bool IsValidDirectory(string? directoryPath, out string? errorMessage)
        {
            return IsValidPath(directoryPath, out errorMessage);
        }
    }
}
