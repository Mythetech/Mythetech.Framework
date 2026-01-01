using System.Diagnostics;
using System.Runtime.InteropServices;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Cross-platform desktop implementation for showing/revealing files and folders
/// </summary>
public class ShowFileService : IShowFileService
{
    /// <inheritdoc />
    public Task ShowFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is empty.", nameof(path));

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found.", fullPath);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{fullPath}\"",
                UseShellExecute = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"-R \"{fullPath}\"");
        }
        else
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("Could not determine directory for file path.");

            Process.Start("xdg-open", $"\"{directory}\"");
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

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{fullPath}\"",
                UseShellExecute = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"\"{fullPath}\"");
        }
        else
        {
            Process.Start("xdg-open", $"\"{fullPath}\"");
        }

        return Task.CompletedTask;
    }
}

