namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// Provides read operations for files.
/// Implementations are platform-specific (Desktop uses System.IO, WebAssembly throws PlatformNotSupportedException).
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Reads the entire contents of a file as a byte array.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The file contents as bytes.</returns>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the file is denied.</exception>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task<byte[]> ReadBytesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the entire contents of a file as a string using UTF-8 encoding.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The file contents as a string.</returns>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the file is denied.</exception>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a file exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata information about a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>File metadata, or null if the file does not exist.</returns>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task<FileMetadata?> GetInfoAsync(string path, CancellationToken cancellationToken = default);
}
