namespace VideoConverter.Common.Models;

/// <summary>
/// Status of a conversion item.
/// </summary>
public enum ConversionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Deleted,
}
