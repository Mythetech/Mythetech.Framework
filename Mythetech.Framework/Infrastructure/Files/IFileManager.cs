namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// Provides file management operations (delete, rename, move, copy).
/// Implementations are platform-specific (Desktop uses System.IO, WebAssembly throws PlatformNotSupportedException).
/// </summary>
public interface IFileManager
{
    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    /// <param name="path">The path to the file to delete.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if the file was deleted, false if it did not exist.</returns>
    /// <exception cref="UnauthorizedAccessException">Access to the file is denied.</exception>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a file (same directory, different name).
    /// </summary>
    /// <param name="path">The current path to the file.</param>
    /// <param name="newName">The new file name (not a full path).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="FileNotFoundException">The source file does not exist.</exception>
    /// <exception cref="IOException">A file with the new name already exists.</exception>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task RenameAsync(string path, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file from one location to another.
    /// </summary>
    /// <param name="sourcePath">The current path to the file.</param>
    /// <param name="destinationPath">The destination path for the file.</param>
    /// <param name="overwrite">Whether to overwrite an existing file at the destination.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="FileNotFoundException">The source file does not exist.</exception>
    /// <exception cref="IOException">A file exists at the destination and overwrite is false.</exception>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file from one location to another.
    /// </summary>
    /// <param name="sourcePath">The path to the file to copy.</param>
    /// <param name="destinationPath">The destination path for the copy.</param>
    /// <param name="overwrite">Whether to overwrite an existing file at the destination.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="FileNotFoundException">The source file does not exist.</exception>
    /// <exception cref="IOException">A file exists at the destination and overwrite is false.</exception>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default);
}
