using System.IO.Compression;

namespace Mythetech.Framework.Infrastructure.Plugins.Installation;

/// <summary>
/// Reads plugin packages: zip archives holding a plugin's DLLs and assets.
/// </summary>
public static class PluginPackageExtractor
{
    /// <summary>
    /// Whether a seekable stream starts with a zip header. The stream's position is left unchanged.
    /// </summary>
    public static async Task<bool> IsZipAsync(Stream stream)
    {
        if (!stream.CanSeek) return false;

        var buffer = new byte[4];
        var originalPosition = stream.Position;

        var bytesRead = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false);
        stream.Position = originalPosition;

        if (bytesRead < 4) return false;

        return buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04;
    }

    /// <summary>
    /// Extracts a zip into <paramref name="targetFolder"/>. When every entry sits under one top-level folder,
    /// that folder is stripped so the plugin's files land directly in the target.
    /// </summary>
    /// <exception cref="InvalidDataException">The stream isn't a zip archive</exception>
    /// <exception cref="InvalidOperationException">An entry would extract outside the target folder</exception>
    public static void Extract(Stream zipStream, string targetFolder)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        var fullTarget = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetFolder));

        var singleRootFolder = GetSingleRootFolder(archive);
        var prefixToStrip = singleRootFolder is not null ? singleRootFolder + "/" : string.Empty;

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var relativePath = entry.FullName;

            if (!string.IsNullOrEmpty(prefixToStrip) && relativePath.StartsWith(prefixToStrip, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath[prefixToStrip.Length..];
            }

            if (string.IsNullOrEmpty(relativePath))
                continue;

            var destinationPath = Path.GetFullPath(Path.Combine(fullTarget, relativePath));

            if (!destinationPath.StartsWith(fullTarget + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException($"Zip entry '{entry.FullName}' would extract outside the target directory.");

            var destinationDir = Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }

    private static string? GetSingleRootFolder(ZipArchive archive)
    {
        string? rootFolder = null;

        foreach (var entry in archive.Entries)
        {
            var parts = entry.FullName.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;

            var topLevel = parts[0];

            if (rootFolder is null)
            {
                rootFolder = topLevel;
            }
            else if (!rootFolder.Equals(topLevel, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        if (rootFolder is null)
            return null;

        var hasFilesAtRoot = archive.Entries.Any(e =>
            !string.IsNullOrEmpty(e.Name) &&
            !e.FullName.Contains('/'));

        return hasFilesAtRoot ? null : rootFolder;
    }
}
