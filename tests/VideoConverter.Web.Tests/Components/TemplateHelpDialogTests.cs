using VideoConverter.Web.Models;
using static VideoConverter.Web.Components.TemplateHelpDialog;

namespace VideoConverter.Web.Tests.Components;

/// <summary>
/// Unit tests for TemplateHelpDialog component logic.
/// Tests tab initialization behavior based on InitialTab parameter.
/// </summary>
public sealed class TemplateHelpDialogTests
{
    /// <summary>
    /// Tests that active panel index is 0 when InitialTab is Filename.
    /// </summary>
    [Fact]
    public void InitialTab_Filename_SetsActivePanelIndexToZero()
    {
        // Arrange
        var initialTab = TemplateType.Filename;

        // Act
        var activePanelIndex = TemplateHelpDialogLogic.GetInitialActivePanelIndex(initialTab);

        // Assert
        activePanelIndex.Should().Be(0, "Filename tab should be at index 0");
    }

    /// <summary>
    /// Tests that active panel index is 1 when InitialTab is Path.
    /// </summary>
    [Fact]
    public void InitialTab_Path_SetsActivePanelIndexToOne()
    {
        // Arrange
        var initialTab = TemplateType.Path;

        // Act
        var activePanelIndex = TemplateHelpDialogLogic.GetInitialActivePanelIndex(initialTab);

        // Assert
        activePanelIndex.Should().Be(1, "Path tab should be at index 1");
    }

    /// <summary>
    /// Tests that all TemplateType values map to valid panel indices.
    /// </summary>
    /// <param name="templateType">The template type to test.</param>
    /// <param name="expectedIndex">The expected panel index.</param>
    [Theory]
    [InlineData(TemplateType.Filename, 0)]
    [InlineData(TemplateType.Path, 1)]
    public void InitialTab_AllValidTypes_MapToCorrectPanelIndex(TemplateType templateType, int expectedIndex)
    {
        // Arrange & Act
        var activePanelIndex = TemplateHelpDialogLogic.GetInitialActivePanelIndex(templateType);

        // Assert
        activePanelIndex.Should().Be(expectedIndex, $"{templateType} should map to panel index {expectedIndex}");
    }
}
