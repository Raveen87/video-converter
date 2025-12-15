namespace VideoConverter.Web.Models;

/// <summary>
/// Helper class for determining selection type from file browser paths.
/// </summary>
public static class PathSelectionHelper
{
    /// <summary>
    /// Determines the selection type based on whether the path represents a file or directory.
    /// </summary>
    /// <param name="path">The selected path.</param>
    /// <param name="isDirectory">Whether the path is a directory.</param>
    /// <returns>The appropriate SelectionType.</returns>
    public static SelectionType DetermineSelectionType(string? path, bool isDirectory)
    {
        if (string.IsNullOrEmpty(path))
        {
            return SelectionType.None;
        }

        return isDirectory ? SelectionType.Directory : SelectionType.File;
    }
}
