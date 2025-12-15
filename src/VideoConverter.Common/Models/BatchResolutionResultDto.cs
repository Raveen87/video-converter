namespace VideoConverter.Common.Models;

/// <summary>
/// Result for batch resolution operations with partial failure support.
/// </summary>
public class BatchResolutionResultDto
{
    /// <summary>
    /// Gets or sets the list of queue item IDs that were successfully resolved.
    /// </summary>
    public IList<int> SuccessfulIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of errors for items that failed to resolve.
    /// </summary>
    public IList<BatchResolutionErrorDto> Errors { get; set; } = [];
}
