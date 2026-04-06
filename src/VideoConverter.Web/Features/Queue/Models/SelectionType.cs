namespace VideoConverter.Web.Features.Queue.Models;

/// <summary>
/// Indicates whether the user has selected a file or a directory in the file browser.
/// </summary>
public enum SelectionType
{
    /// <summary>
    /// No selection has been made.
    /// </summary>
    None,

    /// <summary>
    /// A file has been selected.
    /// </summary>
    File,

    /// <summary>
    /// A directory has been selected.
    /// </summary>
    Directory,
}
