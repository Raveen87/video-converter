using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Common.Models;
using VideoConverter.Web.Features.Queue.Logic;
using VideoConverter.Web.Features.Queue.Models;

namespace VideoConverter.Web.Tests.Components;

/// <summary>
/// Property-based tests for UnifiedQueueDialog component.
/// </summary>
public class UnifiedQueueDialogPropertyTests
{
    /// <summary>
    /// Feature: unified-queue-dialog, Property 3: Extensions field visibility matches selection type.
    /// Validates: Requirements 3.1, 3.2.
    /// For any SelectionType value, the extensions filter field SHALL be visible if and only if SelectionType equals Directory.
    /// </summary>
    /// <param name="selectionTypeInt">A random integer to derive selection type from.</param>
    /// <returns>A property that validates extensions field visibility.</returns>
    [Property(MaxTest = 100)]
    public Property ExtensionsFieldVisibilityMatchesSelectionType(byte selectionTypeInt)
    {
        // Map to valid SelectionType values (0, 1, 2)
        var selectionType = (SelectionType)(selectionTypeInt % 3);

        // Act
        var shouldShowExtensions = UnifiedQueueDialogLogic.ShouldShowExtensionsField(selectionType);

        // Assert - extensions field should only be visible when SelectionType is Directory
        var expected = selectionType == SelectionType.Directory;
        return (shouldShowExtensions == expected)
            .Label($"SelectionType {selectionType}: expected extensions visible={expected}, got {shouldShowExtensions}");
    }

    /// <summary>
    /// Feature: unified-queue-dialog, Property 4: Profile selection populates form fields.
    /// Validates: Requirements 4.2.
    /// For any profile selected from the dropdown, the form fields SHALL contain the corresponding values from that profile's settings.
    /// </summary>
    /// <param name="name">Generated profile name.</param>
    /// <param name="outputDir">Generated output directory.</param>
    /// <param name="crf">Generated CRF value.</param>
    /// <param name="audioBitrate">Generated audio bitrate.</param>
    /// <param name="resolution">Generated resolution.</param>
    /// <param name="template">Generated filename template.</param>
    /// <returns>A property that validates profile selection populates form fields.</returns>
    [Property(MaxTest = 100)]
    public Property ProfileSelectionPopulatesFormFields(
        NonEmptyString name,
        NonEmptyString outputDir,
        PositiveInt crf,
        PositiveInt audioBitrate,
        PositiveInt resolution,
        NonEmptyString template)
    {
        // Arrange
        var profile = new ProfileDto(
            Id: 1,
            Name: name.Item,
            OutputDirectory: outputDir.Item,
            Crf: (crf.Item % 51) + 1, // CRF is typically 1-51
            AudioBitrateKbps: audioBitrate.Item,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: (resolution.Item % 2160) + 480, // Reasonable resolution range
            OutputFilenameTemplate: template.Item,
            OutputPathTemplate: null,
            AudioLanguages: new[] { "eng", "jpn" },
            SubtitleLanguages: new[] { "eng" });

        var formModel = new UnifiedQueueFormModel();

        // Act
        UnifiedQueueDialogLogic.ApplyProfileToForm(profile, formModel);

        // Assert - form fields should match profile values
        var outputDirMatches = string.Equals(formModel.OutputDirectory, profile.OutputDirectory, StringComparison.Ordinal);
        var crfMatches = formModel.Crf == profile.Crf;
        var audioBitrateMatches = formModel.AudioBitrateKbps == profile.AudioBitrateKbps;
        var audioChannelsMatches = formModel.AudioChannels == profile.AudioChannels;
        var resolutionMatches = formModel.Resolution == profile.Resolution;
        var templateMatches = string.Equals(formModel.OutputFilenameTemplate, profile.OutputFilenameTemplate, StringComparison.Ordinal);
        var audioLangsMatches = string.Equals(formModel.AudioLanguagesString, string.Join(", ", profile.AudioLanguages!), StringComparison.Ordinal);
        var subtitleLangsMatches = string.Equals(formModel.SubtitleLanguagesString, string.Join(", ", profile.SubtitleLanguages!), StringComparison.Ordinal);

        return (outputDirMatches && crfMatches && audioBitrateMatches && audioChannelsMatches &&
                resolutionMatches && templateMatches && audioLangsMatches && subtitleLangsMatches)
            .Label($"Profile '{profile.Name}' should populate all form fields correctly");
    }

    /// <summary>
    /// Feature: unified-queue-dialog, Property 5: File submission routes to Conversion endpoint.
    /// Validates: Requirements 5.1.
    /// For any submission where SelectionType is File, the system SHALL invoke the file conversion callback.
    /// </summary>
    /// <param name="filePath">Generated file path.</param>
    /// <param name="outputDir">Generated output directory.</param>
    /// <returns>A property that validates file submission routing.</returns>
    [Property(MaxTest = 100)]
    public Property FileSubmissionRoutesToConversionEndpoint(NonEmptyString filePath, NonEmptyString outputDir)
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel
        {
            SelectedPath = filePath.Item,
            SelectionType = SelectionType.File,
            OutputDirectory = outputDir.Item,
        };

        // Act
        var routingResult = UnifiedQueueDialogLogic.DetermineSubmitRoute(formModel, profileId: null);

        // Assert
        return (routingResult == SubmitRoute.FileConversion)
            .Label($"File selection should route to FileConversion, got {routingResult}");
    }

    /// <summary>
    /// Feature: unified-queue-dialog, Property 6: Directory submission routes to Directory Queue endpoint.
    /// Validates: Requirements 5.2.
    /// For any submission where SelectionType is Directory, the system SHALL invoke the directory queue callback.
    /// </summary>
    /// <param name="directoryPath">Generated directory path.</param>
    /// <param name="outputDir">Generated output directory.</param>
    /// <returns>A property that validates directory submission routing.</returns>
    [Property(MaxTest = 100)]
    public Property DirectorySubmissionRoutesToDirectoryQueueEndpoint(NonEmptyString directoryPath, NonEmptyString outputDir)
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel
        {
            SelectedPath = directoryPath.Item,
            SelectionType = SelectionType.Directory,
            OutputDirectory = outputDir.Item,
        };

        // Act
        var routingResult = UnifiedQueueDialogLogic.DetermineSubmitRoute(formModel, profileId: null);

        // Assert
        return (routingResult == SubmitRoute.DirectoryQueue)
            .Label($"Directory selection should route to DirectoryQueue, got {routingResult}");
    }

    /// <summary>
    /// Feature: unified-queue-dialog, Property 7: Profile file submission includes profile ID.
    /// Validates: Requirements 5.3.
    /// For any submission where SelectionType is File and a profile is selected, the system SHALL invoke the profile-based file conversion callback.
    /// </summary>
    /// <param name="filePath">Generated file path.</param>
    /// <param name="outputDir">Generated output directory.</param>
    /// <param name="profileId">Generated profile ID.</param>
    /// <returns>A property that validates profile file submission routing.</returns>
    [Property(MaxTest = 100)]
    public Property ProfileFileSubmissionIncludesProfileId(NonEmptyString filePath, NonEmptyString outputDir, PositiveInt profileId)
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel
        {
            SelectedPath = filePath.Item,
            SelectionType = SelectionType.File,
            OutputDirectory = outputDir.Item,
        };

        // Act
        var routingResult = UnifiedQueueDialogLogic.DetermineSubmitRoute(formModel, profileId.Item);

        // Assert
        return (routingResult == SubmitRoute.FileConversionWithProfile)
            .Label($"File selection with profile should route to FileConversionWithProfile, got {routingResult}");
    }

    /// <summary>
    /// Feature: unified-queue-dialog, Property 8: Profile directory submission routes to profile endpoint.
    /// Validates: Requirements 5.4.
    /// For any submission where SelectionType is Directory and a profile is selected, the system SHALL invoke the profile-based directory queue callback.
    /// </summary>
    /// <param name="directoryPath">Generated directory path.</param>
    /// <param name="outputDir">Generated output directory.</param>
    /// <param name="profileId">Generated profile ID.</param>
    /// <returns>A property that validates profile directory submission routing.</returns>
    [Property(MaxTest = 100)]
    public Property ProfileDirectorySubmissionRoutesToProfileEndpoint(NonEmptyString directoryPath, NonEmptyString outputDir, PositiveInt profileId)
    {
        // Arrange
        var formModel = new UnifiedQueueFormModel
        {
            SelectedPath = directoryPath.Item,
            SelectionType = SelectionType.Directory,
            OutputDirectory = outputDir.Item,
        };

        // Act
        var routingResult = UnifiedQueueDialogLogic.DetermineSubmitRoute(formModel, profileId.Item);

        // Assert
        return (routingResult == SubmitRoute.DirectoryQueueWithProfile)
            .Label($"Directory selection with profile should route to DirectoryQueueWithProfile, got {routingResult}");
    }
}
