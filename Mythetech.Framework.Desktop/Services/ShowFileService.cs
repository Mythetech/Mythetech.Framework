using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Cross-platform desktop implementation for showing/revealing files and folders
/// </summary>
public class ShowFileService : IShowFileService
{
    private readonly ILogger<ShowFileService> _logger;

    public ShowFileService(ILogger<ShowFileService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ShowFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is empty.", nameof(path));

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found.", fullPath);

        try
        {
            Process? process = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{fullPath}\"",
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                process = Process.Start("open", $"-R \"{fullPath}\"");
            }
            else
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(directory))
                    throw new InvalidOperationException("Could not determine directory for file path.");

                process = Process.Start("xdg-open", directory);
            }

            process?.Dispose();
        }
        catch (Exception ex)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(ex, "Failed to show file: {FullPath}", fullPath);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowFolderAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is empty.", nameof(path));

        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"Directory not found: {fullPath}");

        try
        {
            Process? process = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{fullPath}\"",
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                process = Process.Start("open", $"\"{fullPath}\"");
            }
            else
            {
                process = Process.Start("xdg-open", fullPath);
            }

            process?.Dispose();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to show folder: {fullPath}", ex);
        }

        return Task.CompletedTask;
    }
}

