using VideoConverter.Common.Models;

namespace VideoConverter.Web.Models;

/// <summary>
/// Contains testable logic for the UnifiedQueueDialog component.
/// </summary>
public static class UnifiedQueueDialogLogic
{
    /// <summary>
    /// Determines whether the extensions filter field should be visible based on the selection type.
    /// </summary>
    /// <param name="selectionType">The current selection type.</param>
    /// <returns>True if the extensions field should be visible; otherwise, false.</returns>
    public static bool ShouldShowExtensionsField(SelectionType selectionType)
    {
        return selectionType is SelectionType.Directory;
    }

    /// <summary>
    /// Applies profile settings to the form model.
    /// </summary>
    /// <param name="profile">The profile to apply.</param>
    /// <param name="formModel">The form model to update.</param>
    public static void ApplyProfileToForm(ProfileDto profile, UnifiedQueueFormModel formModel)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(formModel);

        formModel.OutputDirectory = profile.OutputDirectory ?? formModel.OutputDirectory;
        formModel.Crf = profile.Crf;
        formModel.AudioBitrateKbps = profile.AudioBitrateKbps;
        formModel.AudioChannels = profile.AudioChannels;
        formModel.Resolution = profile.Resolution ?? formModel.Resolution;
        formModel.OutputFilenameTemplate = profile.OutputFilenameTemplate;
        formModel.AudioLanguagesString = profile.AudioLanguages is not null
            ? string.Join(", ", profile.AudioLanguages)
            : null;
        formModel.SubtitleLanguagesString = profile.SubtitleLanguages is not null
            ? string.Join(", ", profile.SubtitleLanguages)
            : null;
    }

    /// <summary>
    /// Determines the submit route based on the form model and profile selection.
    /// </summary>
    /// <param name="formModel">The form model containing the selection.</param>
    /// <param name="profileId">The selected profile ID, or null if no profile is selected.</param>
    /// <returns>The appropriate submit route.</returns>
    public static SubmitRoute DetermineSubmitRoute(UnifiedQueueFormModel formModel, int? profileId)
    {
        ArgumentNullException.ThrowIfNull(formModel);

        if (formModel.SelectionType is SelectionType.None || string.IsNullOrEmpty(formModel.SelectedPath))
        {
            return SubmitRoute.None;
        }

        return (formModel.SelectionType, profileId) switch
        {
            (SelectionType.File, null) => SubmitRoute.FileConversion,
            (SelectionType.File, not null) => SubmitRoute.FileConversionWithProfile,
            (SelectionType.Directory, null) => SubmitRoute.DirectoryQueue,
            (SelectionType.Directory, not null) => SubmitRoute.DirectoryQueueWithProfile,
            _ => SubmitRoute.None,
        };
    }
}
