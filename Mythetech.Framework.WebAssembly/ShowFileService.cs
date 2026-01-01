using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.WebAssembly;

/// <summary>
/// WebAssembly implementation of IShowFileService that logs unsupported operations
/// </summary>
public class ShowFileService : IShowFileService
{
    private readonly ILogger<ShowFileService> _logger;

    /// <summary>
    /// Creates a new instance of the WebAssembly show file service
    /// </summary>
    /// <param name="logger">Logger for warning messages</param>
    public ShowFileService(ILogger<ShowFileService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ShowFileAsync(string path)
    {
        _logger.LogWarning("ShowFileAsync is not supported on WebAssembly platform. Path: {Path}", path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowFolderAsync(string path)
    {
        _logger.LogWarning("ShowFolderAsync is not supported on WebAssembly platform. Path: {Path}", path);
        return Task.CompletedTask;
    }
}

