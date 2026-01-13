namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// Provides directory operations.
/// Implementations are platform-specific (Desktop uses System.IO, WebAssembly throws PlatformNotSupportedException).
/// </summary>
public interface IDirectoryOperations
{
    /// <summary>
    /// Creates a directory at the specified path. Creates parent directories if they don't exist.
    /// </summary>
    /// <param name="path">The path of the directory to create.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if a new directory was created, false if it already existed.</returns>
    /// <exception cref="UnauthorizedAccessException">Access to the path is denied.</exception>
    /// <exception cref="PlatformNotSupportedException">Directory operations are not supported on this platform.</exception>
    Task<bool> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a directory and optionally all its contents.
    /// </summary>
    /// <param name="path">The path of the directory to delete.</param>
    /// <param name="recursive">If true, delete all contents; if false, fail if directory is not empty.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if the directory was deleted, false if it did not exist.</returns>
    /// <exception cref="IOException">The directory is not empty and recursive is false.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the directory is denied.</exception>
    /// <exception cref="PlatformNotSupportedException">Directory operations are not supported on this platform.</exception>
    Task<bool> DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the contents of a directory.
    /// </summary>
    /// <param name="path">The path to the directory to list.</param>
    /// <param name="searchPattern">Optional search pattern (e.g., "*.txt"). Defaults to "*" (all entries).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Collection of directory entries (files and subdirectories).</returns>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="PlatformNotSupportedException">Directory operations are not supported on this platform.</exception>
    Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(string path, string searchPattern = "*", CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a directory exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if the directory exists, false otherwise.</returns>
    /// <exception cref="PlatformNotSupportedException">Directory operations are not supported on this platform.</exception>
    Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata information about a directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Directory metadata, or null if the directory does not exist.</returns>
    /// <exception cref="PlatformNotSupportedException">Directory operations are not supported on this platform.</exception>
    Task<DirectoryMetadata?> GetDirectoryInfoAsync(string path, CancellationToken cancellationToken = default);
}
