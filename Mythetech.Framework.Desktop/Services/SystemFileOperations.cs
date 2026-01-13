using System.Text;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Desktop implementation of file operations using System.IO.
/// Provides cross-platform file system access for Windows, macOS, and Linux.
/// </summary>
public class SystemFileOperations : IFileOperations
{
    private readonly ILogger<SystemFileOperations>? _logger;

    /// <summary>
    /// Creates a new instance of the System.IO-based file operations.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public SystemFileOperations(ILogger<SystemFileOperations>? logger = null)
    {
        _logger = logger;
    }

    #region IFileReader

    /// <inheritdoc />
    public async Task<byte[]> ReadBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        _logger?.LogDebug("Reading bytes from {Path}", normalizedPath);
        return await File.ReadAllBytesAsync(normalizedPath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        _logger?.LogDebug("Reading text from {Path}", normalizedPath);
        return await File.ReadAllTextAsync(normalizedPath, Encoding.UTF8, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        return Task.FromResult(File.Exists(normalizedPath));
    }

    /// <inheritdoc />
    public Task<FileMetadata?> GetInfoAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (!File.Exists(normalizedPath))
            return Task.FromResult<FileMetadata?>(null);

        var info = new FileInfo(normalizedPath);
        return Task.FromResult<FileMetadata?>(new FileMetadata(
            Path: info.FullName,
            Name: info.Name,
            Extension: info.Extension,
            Size: info.Length,
            CreatedUtc: info.CreationTimeUtc,
            ModifiedUtc: info.LastWriteTimeUtc,
            IsReadOnly: info.IsReadOnly));
    }

    #endregion

    #region IFileWriter

    /// <inheritdoc />
    public async Task WriteBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        _logger?.LogDebug("Writing {ByteCount} bytes to {Path}", data.Length, normalizedPath);
        await File.WriteAllBytesAsync(normalizedPath, data, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        _logger?.LogDebug("Writing text to {Path}", normalizedPath);
        await File.WriteAllTextAsync(normalizedPath, content, Encoding.UTF8, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> CreateAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (File.Exists(normalizedPath))
        {
            _logger?.LogDebug("File already exists: {Path}", normalizedPath);
            return Task.FromResult(false);
        }

        _logger?.LogDebug("Creating file: {Path}", normalizedPath);
        using var _ = File.Create(normalizedPath);
        return Task.FromResult(true);
    }

    #endregion

    #region IFileManager

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (!File.Exists(normalizedPath))
        {
            _logger?.LogDebug("File does not exist for deletion: {Path}", normalizedPath);
            return Task.FromResult(false);
        }

        _logger?.LogDebug("Deleting file: {Path}", normalizedPath);
        File.Delete(normalizedPath);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task RenameAsync(string path, string newName, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        var directory = Path.GetDirectoryName(normalizedPath)
            ?? throw new InvalidOperationException($"Cannot determine directory for path: {path}");
        var destinationPath = Path.Combine(directory, newName);

        _logger?.LogDebug("Renaming {SourcePath} to {NewName}", normalizedPath, newName);
        File.Move(normalizedPath, destinationPath);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var normalizedSource = NormalizePath(sourcePath);
        var normalizedDest = NormalizePath(destinationPath);

        _logger?.LogDebug("Moving {SourcePath} to {DestPath} (overwrite: {Overwrite})",
            normalizedSource, normalizedDest, overwrite);
        File.Move(normalizedSource, normalizedDest, overwrite);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var normalizedSource = NormalizePath(sourcePath);
        var normalizedDest = NormalizePath(destinationPath);

        _logger?.LogDebug("Copying {SourcePath} to {DestPath} (overwrite: {Overwrite})",
            normalizedSource, normalizedDest, overwrite);
        File.Copy(normalizedSource, normalizedDest, overwrite);
        return Task.CompletedTask;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Normalizes a path to ensure consistent behavior across platforms.
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        return Path.GetFullPath(path);
    }

    #endregion
}
