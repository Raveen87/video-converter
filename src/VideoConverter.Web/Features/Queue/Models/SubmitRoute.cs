namespace VideoConverter.Web.Features.Queue.Models;

/// <summary>
/// Represents the possible submit routes for the unified queue dialog.
/// </summary>
public enum SubmitRoute
{
    /// <summary>
    /// No valid route (no selection or invalid state).
    /// </summary>
    None,

    /// <summary>
    /// Route to file conversion endpoint without profile.
    /// </summary>
    FileConversion,

    /// <summary>
    /// Route to file conversion endpoint with profile.
    /// </summary>
    FileConversionWithProfile,

    /// <summary>
    /// Route to directory queue endpoint without profile.
    /// </summary>
    DirectoryQueue,

    /// <summary>
    /// Route to directory queue endpoint with profile.
    /// </summary>
    DirectoryQueueWithProfile,
}
