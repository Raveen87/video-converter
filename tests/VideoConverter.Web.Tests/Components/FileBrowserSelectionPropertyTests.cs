using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Web.Models;

namespace VideoConverter.Web.Tests.Components;

/// <summary>
/// Property-based tests for file browser selection type determination.
/// Feature: unified-queue-dialog, Property 1: File selection sets SelectionType to File.
/// Feature: unified-queue-dialog, Property 2: Directory selection sets SelectionType to Directory.
/// </summary>
public class FileBrowserSelectionPropertyTests
{
    /// <summary>
    /// Feature: unified-queue-dialog, Property 1: File selection sets SelectionType to File.
    /// Validates: Requirements 2.2.
    /// For any file path selected in the file browser, the SelectionType SHALL be set to SelectionType.File.
    /// </summary>
    /// <param name="filePath">The generated file path.</param>
    /// <returns>A property that validates file selection sets SelectionType to File.</returns>
    [Property(MaxTest = 100)]
    public Property FileSelectionSetsSelectionTypeToFile(NonEmptyString filePath)
    {
        // Arrange - file paths are not directories
        var path = filePath.Item;
        const bool isDirectory = false;

        // Act
        var selectionType = PathSelectionHelper.DetermineSelectionType(path, isDirectory);

        // Assert
        return (selectionType == SelectionType.File)
            .ToProperty()
            .Label($"File path '{path}' should result in SelectionType.File, got {selectionType}");
    }

    /// <summary>
    /// Feature: unified-queue-dialog, Property 2: Directory selection sets SelectionType to Directory.
    /// Validates: Requirements 2.3.
    /// For any directory path selected in the file browser, the SelectionType SHALL be set to SelectionType.Directory.
    /// </summary>
    /// <param name="directoryPath">The generated directory path.</param>
    /// <returns>A property that validates directory selection sets SelectionType to Directory.</returns>
    [Property(MaxTest = 100)]
    public Property DirectorySelectionSetsSelectionTypeToDirectory(NonEmptyString directoryPath)
    {
        // Arrange - directory paths are directories
        var path = directoryPath.Item;
        const bool isDirectory = true;

        // Act
        var selectionType = PathSelectionHelper.DetermineSelectionType(path, isDirectory);

        // Assert
        return (selectionType == SelectionType.Directory)
            .ToProperty()
            .Label($"Directory path '{path}' should result in SelectionType.Directory, got {selectionType}");
    }

    /// <summary>
    /// Property: Empty or null path results in SelectionType.None.
    /// </summary>
    /// <param name="isDirectory">Whether the path is a directory.</param>
    /// <returns>A property that validates empty paths result in SelectionType.None.</returns>
    [Property(MaxTest = 100)]
    public Property EmptyPathSetsSelectionTypeToNone(bool isDirectory)
    {
        // Act - test with null
        var selectionTypeNull = PathSelectionHelper.DetermineSelectionType(null, isDirectory);

        // Act - test with empty string
        var selectionTypeEmpty = PathSelectionHelper.DetermineSelectionType(string.Empty, isDirectory);

        // Assert
        return (selectionTypeNull == SelectionType.None && selectionTypeEmpty == SelectionType.None)
            .ToProperty()
            .Label("Empty or null path should result in SelectionType.None");
    }
}
