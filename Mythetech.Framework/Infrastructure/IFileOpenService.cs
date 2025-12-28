namespace Mythetech.Framework.Infrastructure;

/// <summary>
/// An abstract way to open files and folders for components
/// </summary>
public interface IFileOpenService
{
    /// <summary>
    /// Opens a file dialog to select one or more files
    /// </summary>
    /// <param name="title">The title of the file dialog</param>
    /// <param name="defaultPath">The default path to open the dialog at</param>
    /// <param name="multiSelect">Whether multiple files can be selected</param>
    /// <param name="filters">File filters to apply to the dialog</param>
    /// <returns>An array of selected file paths, or an empty array if cancelled</returns>
    Task<string[]> OpenFileAsync(
        string title = "Choose file",
        string? defaultPath = null,
        bool multiSelect = false,
        FileFilter[]? filters = null);

    /// <summary>
    /// Opens a folder dialog to select one or more folders
    /// </summary>
    /// <param name="title">The title of the folder dialog</param>
    /// <param name="defaultPath">The default path to open the dialog at</param>
    /// <param name="multiSelect">Whether multiple folders can be selected</param>
    /// <returns>An array of selected folder paths, or an empty array if cancelled</returns>
    Task<string[]> OpenFolderAsync(
        string title = "Choose folder",
        string? defaultPath = null,
        bool multiSelect = false);
}

