using VideoConverter.Common.Models;
using VideoConverter.Web.Features.Queue.Logic;
using VideoConverter.Web.Features.Queue.Models;
using Xunit;

namespace VideoConverter.Web.Tests.Components;

/// <summary>
/// Unit tests for UnifiedQueueDialog component logic.
/// Tests component initialization, form field visibility, and submit button state.
/// </summary>
public sealed class UnifiedQueueDialogTests
{
    /// <summary>
    /// Tests that the dialog default state has no path selected.
    /// Requirements: 1.1, 4.1.
    /// </summary>
    [Fact]
    public void Dialog_DefaultState_HasNoPathSelected()
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel();

        // Assert - Default state should have no path selected
        formModel.SelectedPath.Should().BeNull();
        formModel.SelectionType.Should().Be(SelectionType.None);
    }

    /// <summary>
    /// Tests that the dialog default state has empty output directory.
    /// Requirements: 1.1.
    /// </summary>
    [Fact]
    public void Dialog_DefaultState_HasEmptyOutputDirectory()
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel();

        // Assert
        formModel.OutputDirectory.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that profile application populates form fields correctly.
    /// Requirements: 4.1, 4.2.
    /// </summary>
    [Fact]
    public void Dialog_ProfileApplication_PopulatesFormFields()
    {
        // Arrange
        var profile = new ProfileDto(
            Id: 1,
            Name: "Test Profile",
            OutputDirectory: "/output/test",
            Crf: 20,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            OutputFilenameTemplate: "{title}",
            OutputPathTemplate: null,
            AudioLanguages: new[] { "eng", "jpn" },
            SubtitleLanguages: new[] { "eng" });

        var formModel = new UnifiedQueueFormModel();

        // Act
        UnifiedQueueDialogLogic.ApplyProfileToForm(profile, formModel);

        // Assert
        formModel.OutputDirectory.Should().Be("/output/test");
        formModel.Crf.Should().Be(20);
        formModel.AudioBitrateKbps.Should().Be(128);
        formModel.AudioChannels.Should().Be(AudioChannels.Stereo20);
        formModel.Resolution.Should().Be(1080);
        formModel.OutputFilenameTemplate.Should().Be("{title}");
        formModel.AudioLanguagesString.Should().Be("eng, jpn");
        formModel.SubtitleLanguagesString.Should().Be("eng");
    }

    /// <summary>
    /// Tests that extensions field is hidden when file is selected.
    /// Requirements: 3.1, 3.2.
    /// </summary>
    [Fact]
    public void ExtensionsField_HiddenWhenFileSelected()
    {
        // Arrange & Act
        var result = UnifiedQueueDialogLogic.ShouldShowExtensionsField(SelectionType.File);

        // Assert
        result.Should().BeFalse("extensions field should be hidden when a file is selected");
    }

    /// <summary>
    /// Tests that extensions field is visible when directory is selected.
    /// Requirements: 3.1, 3.2.
    /// </summary>
    [Fact]
    public void ExtensionsField_VisibleWhenDirectorySelected()
    {
        // Arrange & Act
        var result = UnifiedQueueDialogLogic.ShouldShowExtensionsField(SelectionType.Directory);

        // Assert
        result.Should().BeTrue("extensions field should be visible when a directory is selected");
    }

    /// <summary>
    /// Tests that extensions field is hidden when no selection is made.
    /// Requirements: 3.1, 3.2.
    /// </summary>
    [Fact]
    public void ExtensionsField_HiddenWhenNoSelection()
    {
        // Arrange & Act
        var result = UnifiedQueueDialogLogic.ShouldShowExtensionsField(SelectionType.None);

        // Assert
        result.Should().BeFalse("extensions field should be hidden when no selection is made");
    }

    /// <summary>
    /// Tests that submit button is disabled when no path is selected.
    /// Requirements: 1.3.
    /// </summary>
    [Fact]
    public void SubmitButton_DisabledWhenNoPathSelected()
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel
        {
            SelectedPath = null,
            SelectionType = SelectionType.None,
        };

        // Act
        var route = UnifiedQueueDialogLogic.DetermineSubmitRoute(formModel, profileId: null);

        // Assert
        route.Should().Be(SubmitRoute.None, "submit should not be possible when no path is selected");
    }

    /// <summary>
    /// Tests that submit button is enabled when a file path is selected.
    /// Requirements: 1.3.
    /// </summary>
    [Fact]
    public void SubmitButton_EnabledWhenFilePathSelected()
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel
        {
            SelectedPath = "/path/to/file.mp4",
            SelectionType = SelectionType.File,
            OutputDirectory = "/output",
        };

        // Act
        var route = UnifiedQueueDialogLogic.DetermineSubmitRoute(formModel, profileId: null);

        // Assert
        route.Should().Be(SubmitRoute.FileConversion, "submit should route to file conversion when file is selected");
    }

    /// <summary>
    /// Tests that submit button is enabled when a directory path is selected.
    /// Requirements: 1.3.
    /// </summary>
    [Fact]
    public void SubmitButton_EnabledWhenDirectoryPathSelected()
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel
        {
            SelectedPath = "/path/to/directory",
            SelectionType = SelectionType.Directory,
            OutputDirectory = "/output",
        };

        // Act
        var route = UnifiedQueueDialogLogic.DetermineSubmitRoute(formModel, profileId: null);

        // Assert
        route.Should().Be(SubmitRoute.DirectoryQueue, "submit should route to directory queue when directory is selected");
    }

    /// <summary>
    /// Tests that submit is disabled when path is set but selection type is None.
    /// Requirements: 1.3.
    /// </summary>
    [Fact]
    public void SubmitButton_DisabledWhenSelectionTypeIsNone()
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel
        {
            SelectedPath = "/some/path",
            SelectionType = SelectionType.None,
        };

        // Act
        var route = UnifiedQueueDialogLogic.DetermineSubmitRoute(formModel, profileId: null);

        // Assert
        route.Should().Be(SubmitRoute.None, "submit should not be possible when selection type is None");
    }

    /// <summary>
    /// Tests that submit is disabled when selection type is set but path is empty.
    /// Requirements: 1.3.
    /// </summary>
    [Fact]
    public void SubmitButton_DisabledWhenPathIsEmpty()
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel
        {
            SelectedPath = string.Empty,
            SelectionType = SelectionType.File,
        };

        // Act
        var route = UnifiedQueueDialogLogic.DetermineSubmitRoute(formModel, profileId: null);

        // Assert
        route.Should().Be(SubmitRoute.None, "submit should not be possible when path is empty");
    }
}
