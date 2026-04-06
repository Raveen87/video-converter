using VideoConverter.Web.Features.Conversions.Logic;
using Xunit;

namespace VideoConverter.Web.Tests.Components;

/// <summary>
/// Unit tests for EditConversionDialogLogic Enter key handling.
/// Requirements: 2.1, 2.2.
/// </summary>
public class EditConversionDialogLogicTests
{
    [Fact]
    public void ShouldTriggerSubmit_EnterKeyNotSubmitting_ReturnsTrue()
    {
        // Arrange
        const string key = "Enter";
        const bool isSubmitting = false;

        // Act
        var result = EditConversionDialogLogic.ShouldTriggerSubmit(key, isSubmitting);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldTriggerSubmit_EnterKeyWhileSubmitting_ReturnsFalse()
    {
        // Arrange
        const string key = "Enter";
        const bool isSubmitting = true;

        // Act
        var result = EditConversionDialogLogic.ShouldTriggerSubmit(key, isSubmitting);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("Tab")]
    [InlineData("Escape")]
    [InlineData("Space")]
    [InlineData("a")]
    [InlineData("ArrowDown")]
    public void ShouldTriggerSubmit_NonEnterKey_ReturnsFalse(string key)
    {
        // Arrange
        const bool isSubmitting = false;

        // Act
        var result = EditConversionDialogLogic.ShouldTriggerSubmit(key, isSubmitting);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldTriggerSubmit_NullKey_ReturnsFalse()
    {
        // Arrange
        const bool isSubmitting = false;

        // Act
        var result = EditConversionDialogLogic.ShouldTriggerSubmit(null, isSubmitting);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldAllowSubmission_EnterKeyValidFormNotSubmitting_ReturnsTrue()
    {
        // Arrange
        const string key = "Enter";
        const bool isSubmitting = false;
        const bool isFormValid = true;

        // Act
        var result = EditConversionDialogLogic.ShouldAllowSubmission(key, isSubmitting, isFormValid);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldAllowSubmission_EnterKeyInvalidForm_ReturnsFalse()
    {
        // Arrange
        const string key = "Enter";
        const bool isSubmitting = false;
        const bool isFormValid = false;

        // Act
        var result = EditConversionDialogLogic.ShouldAllowSubmission(key, isSubmitting, isFormValid);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldAllowSubmission_EnterKeyValidFormWhileSubmitting_ReturnsFalse()
    {
        // Arrange
        const string key = "Enter";
        const bool isSubmitting = true;
        const bool isFormValid = true;

        // Act
        var result = EditConversionDialogLogic.ShouldAllowSubmission(key, isSubmitting, isFormValid);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("Tab")]
    [InlineData("Escape")]
    [InlineData("Space")]
    public void ShouldAllowSubmission_NonEnterKeyValidForm_ReturnsFalse(string key)
    {
        // Arrange
        const bool isSubmitting = false;
        const bool isFormValid = true;

        // Act
        var result = EditConversionDialogLogic.ShouldAllowSubmission(key, isSubmitting, isFormValid);

        // Assert
        Assert.False(result);
    }
}
