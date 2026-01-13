using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Desktop implementation of directory operations using System.IO.
/// Provides cross-platform directory access for Windows, macOS, and Linux.
/// </summary>
public class SystemDirectoryOperations : IDirectoryOperations
{
    private readonly ILogger<SystemDirectoryOperations>? _logger;

    /// <summary>
    /// Creates a new instance of the System.IO-based directory operations.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public SystemDirectoryOperations(ILogger<SystemDirectoryOperations>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (Directory.Exists(normalizedPath))
        {
            _logger?.LogDebug("Directory already exists: {Path}", normalizedPath);
            return Task.FromResult(false);
        }

        _logger?.LogDebug("Creating directory: {Path}", normalizedPath);
        Directory.CreateDirectory(normalizedPath);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (!Directory.Exists(normalizedPath))
        {
            _logger?.LogDebug("Directory does not exist for deletion: {Path}", normalizedPath);
            return Task.FromResult(false);
        }

        _logger?.LogDebug("Deleting directory: {Path} (recursive: {Recursive})", normalizedPath, recursive);
        Directory.Delete(normalizedPath, recursive);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(string path, string searchPattern = "*", CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (!Directory.Exists(normalizedPath))
            throw new DirectoryNotFoundException($"Directory not found: {normalizedPath}");

        _logger?.LogDebug("Listing directory: {Path} with pattern: {Pattern}", normalizedPath, searchPattern);

        var entries = new List<DirectoryEntry>();

        // Add directories
        foreach (var dir in Directory.EnumerateDirectories(normalizedPath, searchPattern))
        {
            entries.Add(new DirectoryEntry(
                Path: dir,
                Name: Path.GetFileName(dir),
                IsDirectory: true));
        }

        // Add files
        foreach (var file in Directory.EnumerateFiles(normalizedPath, searchPattern))
        {
            entries.Add(new DirectoryEntry(
                Path: file,
                Name: Path.GetFileName(file),
                IsDirectory: false));
        }

        return Task.FromResult<IReadOnlyList<DirectoryEntry>>(entries);
    }

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        return Task.FromResult(Directory.Exists(normalizedPath));
    }

    /// <inheritdoc />
    public Task<DirectoryMetadata?> GetDirectoryInfoAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (!Directory.Exists(normalizedPath))
            return Task.FromResult<DirectoryMetadata?>(null);

        var info = new DirectoryInfo(normalizedPath);
        var isEmpty = !Directory.EnumerateFileSystemEntries(normalizedPath).Any();

        return Task.FromResult<DirectoryMetadata?>(new DirectoryMetadata(
            Path: info.FullName,
            Name: info.Name,
            CreatedUtc: info.CreationTimeUtc,
            ModifiedUtc: info.LastWriteTimeUtc,
            IsEmpty: isEmpty));
    }

    /// <summary>
    /// Normalizes a path to ensure consistent behavior across platforms.
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        return Path.GetFullPath(path);
    }
}
