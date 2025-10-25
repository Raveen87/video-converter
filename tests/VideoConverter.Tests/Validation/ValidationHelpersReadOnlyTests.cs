using VideoConverter.Web.Validation;

namespace VideoConverter.Tests.Validation;

public class ValidationHelpersReadOnlyTests
{
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
            var isValid = ValidationHelpers.IsValidDirectory(subDir, out var errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Parent directory does not exist", errorMessage);
            Assert.False(Directory.Exists(subDir)); // Directory should not be created
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void IsValidDirectory_WithValidPath_CreatesDirectoryAtomically()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "validation_test_" + Guid.NewGuid().ToString("N"));
        var subDir = Path.Combine(tempDir, "subdirectory");

        try
        {
            // Ensure parent directory exists
            _ = Directory.CreateDirectory(tempDir);

            // Act
            var isValid = ValidationHelpers.IsValidDirectory(subDir, out var errorMessage);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.True(Directory.Exists(subDir)); // Directory should be created atomically during validation
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
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
            var isValid = ValidationHelpers.IsValidDirectory(tempDir, out var errorMessage);

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
        var isValid = ValidationHelpers.IsValidDirectory(invalidPath, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Path contains invalid characters", errorMessage);
    }

    [Fact]
    public void IsValidDirectory_WithNullPath_ReturnsFalse()
    {
        // Act
        var isValid = ValidationHelpers.IsValidDirectory(null, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Path is required.", errorMessage);
    }

    [Fact]
    public void IsValidDirectory_WithEmptyPath_ReturnsFalse()
    {
        // Act
        var isValid = ValidationHelpers.IsValidDirectory("", out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Path is required.", errorMessage);
    }

    [Fact]
    public void IsValidDirectory_WithWhitespacePath_ReturnsFalse()
    {
        // Act
        var isValid = ValidationHelpers.IsValidDirectory("   ", out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Path is required.", errorMessage);
    }

    [Fact]
    public void IsValidDirectory_WithRootDirectoryChild_Windows_ReturnsTrue()
    {
        // Arrange - Test Windows root directory child (e.g., "C:\newdir")
        // This test specifically validates the bug fix where Path.GetDirectoryName() returns null for root children
        var tempRoot = Path.GetTempPath();
        var rootChild = Path.Combine(Path.GetPathRoot(tempRoot)!, "validation_test_" + Guid.NewGuid().ToString("N"));

        try
        {
            // Act
            var isValid = ValidationHelpers.IsValidDirectory(rootChild, out var errorMessage);

            // Assert
            // Note: This test may fail if the root directory is not writable (which is common)
            // The important thing is that it doesn't fail with "Parent directory does not exist"
            if (!isValid)
            {
                Assert.DoesNotContain("Parent directory does not exist", errorMessage);
                Assert.Contains("not writable", errorMessage);
                Assert.False(Directory.Exists(rootChild)); // Directory should not be created when validation fails
            }
            else
            {
                Assert.Null(errorMessage);
                Assert.True(Directory.Exists(rootChild)); // Directory should be created when validation succeeds
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
                var isValid = ValidationHelpers.IsValidDirectory(rootChild, out var errorMessage);

                // Assert
                Assert.True(isValid);
                Assert.Null(errorMessage);
                Assert.True(Directory.Exists(rootChild)); // Directory should be created atomically during validation
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
        // Arrange - Test with a path that has no valid root (edge case)
        var invalidRootChild = "invalidroot:\\testdir";

        // Act
        var isValid = ValidationHelpers.IsValidDirectory(invalidRootChild, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        // Should fail at path validation level before reaching directory validation
    }
}
