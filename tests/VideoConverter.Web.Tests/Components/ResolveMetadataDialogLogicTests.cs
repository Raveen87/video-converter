using VideoConverter.Web.Features.ManualResolution.Logic;
using Xunit;

namespace VideoConverter.Web.Tests.Components;

/// <summary>
/// Unit tests for ResolveMetadataDialogLogic Enter key handling.
/// Requirements: 1.1, 1.2, 1.3, 1.4.
/// </summary>
public class ResolveMetadataDialogLogicTests
{
    [Fact]
    public void ShouldTriggerSearch_EnterKeyWithNonEmptyQuery_ReturnsTrue()
    {
        // Arrange
        const string key = "Enter";
        const bool isSearching = false;
        const string searchQuery = "test query";

        // Act
        var result = ResolveMetadataDialogLogic.ShouldTriggerSearch(key, isSearching, searchQuery);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(null)]
    public void ShouldTriggerSearch_EnterKeyWithEmptyOrWhitespaceQuery_ReturnsFalse(string? searchQuery)
    {
        // Arrange
        const string key = "Enter";
        const bool isSearching = false;

        // Act
        var result = ResolveMetadataDialogLogic.ShouldTriggerSearch(key, isSearching, searchQuery);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldTriggerSearch_EnterKeyWhileSearching_ReturnsFalse()
    {
        // Arrange
        const string key = "Enter";
        const bool isSearching = true;
        const string searchQuery = "test query";

        // Act
        var result = ResolveMetadataDialogLogic.ShouldTriggerSearch(key, isSearching, searchQuery);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("Tab")]
    [InlineData("Escape")]
    [InlineData("Space")]
    [InlineData("a")]
    [InlineData("ArrowDown")]
    public void ShouldTriggerSearch_NonEnterKey_ReturnsFalse(string key)
    {
        // Arrange
        const bool isSearching = false;
        const string searchQuery = "test query";

        // Act
        var result = ResolveMetadataDialogLogic.ShouldTriggerSearch(key, isSearching, searchQuery);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldTriggerSearch_NullKey_ReturnsFalse()
    {
        // Arrange
        const bool isSearching = false;
        const string searchQuery = "test query";

        // Act
        var result = ResolveMetadataDialogLogic.ShouldTriggerSearch(null, isSearching, searchQuery);

        // Assert
        Assert.False(result);
    }
}
