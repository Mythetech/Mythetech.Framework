namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// Provides write operations for files.
/// Implementations are platform-specific (Desktop uses System.IO, WebAssembly throws PlatformNotSupportedException).
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// Writes a byte array to a file, creating the file if it doesn't exist or overwriting if it does.
    /// </summary>
    /// <param name="path">The path to the file to write.</param>
    /// <param name="data">The data to write.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Access to the file is denied.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task WriteBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a string to a file using UTF-8 encoding, creating the file if it doesn't exist or overwriting if it does.
    /// </summary>
    /// <param name="path">The path to the file to write.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Access to the file is denied.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an empty file at the specified path. Does nothing if the file already exists.
    /// </summary>
    /// <param name="path">The path where the file should be created.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if a new file was created, false if it already existed.</returns>
    /// <exception cref="UnauthorizedAccessException">Access to the path is denied.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="PlatformNotSupportedException">File operations are not supported on this platform.</exception>
    Task<bool> CreateAsync(string path, CancellationToken cancellationToken = default);
}
