namespace VideoConverter.Common.Models;

/// <summary>
/// Type of manual resolution required for a queue item.
/// </summary>
public enum ManualResolutionType
{
    /// <summary>
    /// Metadata lookup failed - user needs to select the correct title from search results.
    /// </summary>
    Metadata,

    /// <summary>
    /// Required audio/subtitle streams are missing - user needs to manually select streams.
    /// </summary>
    StreamSelection,
}
