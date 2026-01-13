using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.WebAssembly;

/// <summary>
/// WebAssembly stub for file operations that throws PlatformNotSupportedException.
/// Direct file system access is not available in browser environments.
/// For file access in WebAssembly, use IFileOpenService and IFileSaveService
/// which provide dialog-based file access via the File System Access API.
/// </summary>
public class UnsupportedFileOperations : IFileOperations
{
    private readonly ILogger<UnsupportedFileOperations>? _logger;

    /// <summary>
    /// Creates a new instance of the unsupported file operations service.
    /// </summary>
    /// <param name="logger">Optional logger for warning messages.</param>
    public UnsupportedFileOperations(ILogger<UnsupportedFileOperations>? logger = null)
    {
        _logger = logger;
    }

    private PlatformNotSupportedException CreateException(string operation)
    {
        _logger?.LogWarning("File operation '{Operation}' is not supported on WebAssembly platform. Use IFileOpenService/IFileSaveService for dialog-based file access.", operation);
        return new PlatformNotSupportedException(
            $"Direct file system access is not supported on WebAssembly. " +
            $"Operation: {operation}. Use IFileOpenService/IFileSaveService for dialog-based file access.");
    }

    #region IFileReader

    /// <inheritdoc />
    public Task<byte[]> ReadBytesAsync(string path, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(ReadBytesAsync));

    /// <inheritdoc />
    public Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(ReadTextAsync));

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(ExistsAsync));

    /// <inheritdoc />
    public Task<FileMetadata?> GetInfoAsync(string path, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(GetInfoAsync));

    #endregion

    #region IFileWriter

    /// <inheritdoc />
    public Task WriteBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(WriteBytesAsync));

    /// <inheritdoc />
    public Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(WriteTextAsync));

    /// <inheritdoc />
    public Task<bool> CreateAsync(string path, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(CreateAsync));

    #endregion

    #region IFileManager

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(DeleteAsync));

    /// <inheritdoc />
    public Task RenameAsync(string path, string newName, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(RenameAsync));

    /// <inheritdoc />
    public Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(MoveAsync));

    /// <inheritdoc />
    public Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(CopyAsync));

    #endregion
}
