using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.WebAssembly;

/// <summary>
/// WebAssembly stub for directory operations that throws PlatformNotSupportedException.
/// Direct directory access is not available in browser environments.
/// </summary>
public class UnsupportedDirectoryOperations : IDirectoryOperations
{
    private readonly ILogger<UnsupportedDirectoryOperations>? _logger;

    /// <summary>
    /// Creates a new instance of the unsupported directory operations service.
    /// </summary>
    /// <param name="logger">Optional logger for warning messages.</param>
    public UnsupportedDirectoryOperations(ILogger<UnsupportedDirectoryOperations>? logger = null)
    {
        _logger = logger;
    }

    private PlatformNotSupportedException CreateException(string operation)
    {
        _logger?.LogWarning("Directory operation '{Operation}' is not supported on WebAssembly platform.", operation);
        return new PlatformNotSupportedException(
            $"Direct directory access is not supported on WebAssembly. Operation: {operation}.");
    }

    /// <inheritdoc />
    public Task<bool> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(CreateDirectoryAsync));

    /// <inheritdoc />
    public Task<bool> DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(DeleteDirectoryAsync));

    /// <inheritdoc />
    public Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(string path, string searchPattern = "*", CancellationToken cancellationToken = default)
        => throw CreateException(nameof(ListDirectoryAsync));

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(DirectoryExistsAsync));

    /// <inheritdoc />
    public Task<DirectoryMetadata?> GetDirectoryInfoAsync(string path, CancellationToken cancellationToken = default)
        => throw CreateException(nameof(GetDirectoryInfoAsync));
}
