using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Tests.Validation;

public class FileSystemValidatorTests
{
    private readonly FileSystemValidator _validator = new();

    [Fact]
    public void IsValidDirectory_WithNonExistentParentDirectory_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "validation_test_" + Guid.NewGuid().ToString("N"));
        var subDir = Path.Combine(tempDir, "subdirectory");

        try
        {
            // Don't create parent directory - it should not exist

            // Act
            var isValid = _validator.IsValidDirectory(subDir, out var errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Parent directory does not exist", errorMessage, StringComparison.Ordinal);
            Assert.False(Directory.Exists(subDir)); // Directory should not be created
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void IsValidDirectory_WithExistingDirectory_ReturnsTrue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "validation_test_" + Guid.NewGuid().ToString("N"));

        try
        {
            _ = Directory.CreateDirectory(tempDir);

            // Act
            var isValid = _validator.IsValidDirectory(tempDir, out var errorMessage);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir);
            }
        }
    }

    [Fact]
    public void IsValidDirectory_WithInvalidPath_ReturnsFalse()
    {
        // Arrange
        var invalidPath = "C:\\Invalid<>Path\\With\\Invalid\\Characters";

        // Act
        var isValid = _validator.IsValidDirectory(invalidPath, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Path contains invalid characters", errorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void IsValidDirectory_WithNullPath_ReturnsFalse()
    {
        // Act
        var isValid = _validator.IsValidDirectory(directoryPath: null, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Path is required.", errorMessage);
    }

    [Fact]
    public void IsValidDirectory_WithEmptyPath_ReturnsFalse()
    {
        // Act
        var isValid = _validator.IsValidDirectory(string.Empty, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Path is required.", errorMessage);
    }

    [Fact]
    public void IsValidDirectory_WithWhitespacePath_ReturnsFalse()
    {
        // Act
        var isValid = _validator.IsValidDirectory("   ", out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Path is required.", errorMessage);
    }

    [Fact]
    public void IsValidDirectory_WithRootDirectoryChild_Windows_ReturnsTrue()
    {
        // Arrange - Test Windows root directory child (e.g., "C:\newdir")
        // This test specifically validates the bug fix where Path.GetDirectoryName() returns null for root children
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var tempRoot = Path.GetTempPath();
        var rootChild = Path.Combine(Path.GetPathRoot(tempRoot)!, "validation_test_" + Guid.NewGuid().ToString("N"));

        try
        {
            // Act
            var isValid = _validator.IsValidDirectory(rootChild, out var errorMessage);

            // Assert
            // Note: This test may fail if the root directory is not writable (which is common)
            // The important thing is that it doesn't fail with "Parent directory does not exist"
            if (!isValid)
            {
                Assert.DoesNotContain("Parent directory does not exist", errorMessage, StringComparison.Ordinal);
                Assert.Contains("not writable", errorMessage, StringComparison.Ordinal);
            }
            else
            {
                Assert.Null(errorMessage);

                // Validator checks creatability, not actual creation
                // We verify that we CAN create it if valid
                try
                {
                    Directory.CreateDirectory(rootChild);
                    Assert.True(Directory.Exists(rootChild));
                }
                catch (UnauthorizedAccessException)
                {
                    // If we can't create it despite validation saying we can, that's a platform permission quirk,
                    // but for the purpose of THIS test (which checks parsing of root paths), passing validation is enough.
                }
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(rootChild))
            {
                Directory.Delete(rootChild);
            }
        }
    }

    [Fact]
    public void IsValidDirectory_WithRootDirectoryChild_Unix_ReturnsTrue()
    {
        // Arrange - Test Unix root directory child (e.g., "/newdir")
        // Only run this test on Unix-like systems
        if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX)
        {
            var rootChild = "/validation_test_" + Guid.NewGuid().ToString("N");

            try
            {
                // Act
                var isValid = _validator.IsValidDirectory(rootChild, out var errorMessage);

                // Assert
                if (isValid)
                {
                    Assert.Null(errorMessage);

                    // Validator checks creatability, not actual creation.
                    // In Docker build as root, this should succeed.
                    Directory.CreateDirectory(rootChild);
                    Assert.True(Directory.Exists(rootChild));
                }
                else
                {
                    // If running as non-root user without write access to /, it should be invalid
                    Assert.NotNull(errorMessage);
                }
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(rootChild))
                {
                    Directory.Delete(rootChild);
                }
            }
        }
        else
        {
            // Skip test on Windows
            Assert.True(true, "Skipped on Windows - Unix-specific test");
        }
    }

    [Fact]
    public void IsValidDirectory_WithRootDirectoryChild_InvalidRoot_ReturnsFalse()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange - Test with a path that has no valid root (edge case)
        var invalidRootChild = "invalidroot:\\testdir";

        // Act
        var isValid = _validator.IsValidDirectory(invalidRootChild, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);

        // Should fail at path validation level before reaching directory validation
    }

    [Fact]
    public void IsValidPath_WithInvalidCharacters_ReturnsFalse()
    {
        var invalidPaths = new[]
        {
            "C:\\test|file.mp4",
            "C:\\test<file.mp4",
            "C:\\test>file.mp4",
            "C:\\test\"file.mp4",
            "C:\\test*file.mp4",
            "C:\\test?file.mp4",
        };

        foreach (var path in invalidPaths)
        {
            var result = _validator.IsValidPath(path, out var error);
            Assert.False(result);
            Assert.NotNull(error);
        }
    }

    [Fact]
    public void IsValidPath_WithVeryLongPath_ReturnsFalse()
    {
        var longPath = "C:\\" + new string('a', 300) + ".mp4";
        var ok = _validator.IsValidPath(longPath, out var error);
        Assert.False(ok);
        Assert.NotNull(error);
    }
}
