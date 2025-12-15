namespace VideoConverter.Common.Models;

/// <summary>
/// Error details for a failed batch resolution item.
/// </summary>
public class BatchResolutionErrorDto
{
    /// <summary>
    /// Gets or sets the queue item ID that failed to resolve.
    /// </summary>
    public int QueueItemId { get; set; }

    /// <summary>
    /// Gets or sets the error message describing why the resolution failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
