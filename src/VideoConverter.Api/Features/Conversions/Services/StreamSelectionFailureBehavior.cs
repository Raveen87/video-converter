namespace VideoConverter.Api.Features.Conversions.Services;

/// <summary>
/// Behavior when required audio/subtitle streams are not found.
/// </summary>
public enum StreamSelectionFailureBehavior
{
    /// <summary>
    /// Throw an error and fail the conversion creation.
    /// </summary>
    Fail,

    /// <summary>
    /// Queue the item for manual stream selection by the user.
    /// </summary>
    QueueForManualResolution,

    /// <summary>
    /// Skip the item entirely.
    /// </summary>
    SkipItem,
}
