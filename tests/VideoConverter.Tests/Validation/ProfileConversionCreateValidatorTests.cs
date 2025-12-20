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
            _fileSystemValidator);
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
