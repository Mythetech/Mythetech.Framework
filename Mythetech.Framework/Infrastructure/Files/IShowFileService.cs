namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// An abstract way to show/reveal files and folders in the file system
/// </summary>
public interface IShowFileService
{
    /// <summary>
    /// Reveals/selects a file in its folder in the file manager
    /// </summary>
    /// <param name="path">The path to the file to reveal</param>
    Task ShowFileAsync(string path);

    /// <summary>
    /// Opens a folder in the file manager
    /// </summary>
    /// <param name="path">The path to the folder to open</param>
    Task ShowFolderAsync(string path);
}

