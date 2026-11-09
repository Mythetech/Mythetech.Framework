using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.Plugins.Installation;

/// <summary>
/// The on-disk layout of installed plugins. Each install goes into a new <c>{key}/install-{timestamp}</c> folder,
/// and a retired folder is marked and deleted at the next startup, so a loaded DLL is never deleted.
/// </summary>
public static class PluginInstallLayout
{
    /// <summary>
    /// Name prefix of a versioned install folder under a plugin's key folder.
    /// </summary>
    public const string InstallFolderPrefix = "install-";

    /// <summary>
    /// File that marks a folder for deletion at the next startup.
    /// </summary>
    public const string RemovedMarkerFileName = ".removed";

    private const string TimestampFormat = "yyyyMMddHHmmssfffffff";
    private const string DllPattern = "*.dll";
    private static readonly Lock CreateLock = new();

    /// <summary>
    /// Creates a new, empty install folder for a plugin, named so the newest install sorts last.
    /// </summary>
    /// <param name="pluginsDirectory">The plugins directory</param>
    /// <param name="pluginKey">Key naming the plugin's folder, such as its registry name</param>
    /// <returns>The full path of the created folder</returns>
    public static string CreateInstallFolder(string pluginsDirectory, string pluginKey)
    {
        var keyFolder = Path.Combine(Path.GetFullPath(pluginsDirectory), SanitizeFolderName(pluginKey));

        lock (CreateLock)
        {
            // Moving to the next tick keeps names unique and in creation order when two calls share a tick.
            var timestamp = DateTime.UtcNow;
            string folder;
            while (Directory.Exists(folder = Path.Combine(keyFolder, InstallFolderPrefix + timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture))))
            {
                timestamp = timestamp.AddTicks(1);
            }

            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    /// <summary>
    /// Marks a folder for deletion at the next startup. Does nothing when the folder doesn't exist.
    /// </summary>
    public static void MarkForRemoval(string folder)
    {
        if (!Directory.Exists(folder))
            return;

        File.WriteAllText(Path.Combine(folder, RemovedMarkerFileName), string.Empty);
    }

    /// <summary>
    /// Deletes marked and superseded folders, then returns the folders to load: the newest install folder
    /// of each plugin, each legacy plugin folder holding DLLs directly, and the plugins directory itself when it
    /// holds loose DLLs. Call before loading anything from the directory.
    /// </summary>
    /// <param name="pluginsDirectory">The plugins directory</param>
    /// <param name="logger">Receives a warning for each folder that couldn't be deleted</param>
    public static IReadOnlyList<string> PrepareForLoad(string pluginsDirectory, ILogger logger)
    {
        var folders = new List<string>();
        if (!Directory.Exists(pluginsDirectory))
            return folders;

        if (HasDlls(pluginsDirectory))
            folders.Add(pluginsDirectory);

        foreach (var pluginFolder in SortedDirectories(pluginsDirectory))
        {
            var installs = new List<string>();
            foreach (var install in SortedDirectories(pluginFolder).Where(IsInstallFolder))
            {
                if (!IsMarked(install))
                    installs.Add(install);
                else
                    TryDelete(install, logger);
            }

            foreach (var child in SortedDirectories(pluginFolder).Where(child => !IsInstallFolder(child) && IsMarked(child)))
            {
                TryDelete(child, logger);
            }

            if (IsMarked(pluginFolder))
            {
                // Installs inside a retired legacy folder are newer than it, so only its legacy contents go.
                if (installs.Count == 0)
                {
                    TryDelete(pluginFolder, logger);
                    continue;
                }

                TryRetireLegacyContents(pluginFolder, logger);
            }

            if (installs.Count > 0)
            {
                foreach (var superseded in installs.SkipLast(1))
                {
                    TryDelete(superseded, logger);
                }

                folders.Add(installs[^1]);
            }
            else if (HasDlls(pluginFolder))
            {
                folders.Add(pluginFolder);
            }
        }

        return folders;
    }

    /// <summary>
    /// Removes the characters a folder name can't contain.
    /// </summary>
    public static string SanitizeFolderName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
    }

    /// <summary>
    /// Whether <paramref name="folder"/> is below <paramref name="pluginsDirectory"/>. The directory itself is not inside.
    /// </summary>
    public static bool IsInside(string folder, string pluginsDirectory)
    {
        var normalizedFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pluginsDirectory));

        return normalizedFolder.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInstallFolder(string folder) =>
        Path.GetFileName(folder).StartsWith(InstallFolderPrefix, StringComparison.Ordinal);

    private static bool IsMarked(string folder) => File.Exists(Path.Combine(folder, RemovedMarkerFileName));

    private static bool HasDlls(string folder) => Directory.EnumerateFiles(folder, DllPattern).Any();

    private static IEnumerable<string> SortedDirectories(string folder) =>
        Directory.GetDirectories(folder).Order(StringComparer.Ordinal);

    private static void TryDelete(string folder, ILogger logger)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete retired plugin folder {Folder}; it won't be loaded", folder);
        }
    }

    // The marker goes last, so a failure leaves the folder marked and the next startup tries again.
    private static void TryRetireLegacyContents(string folder, ILogger logger)
    {
        try
        {
            var marker = Path.Combine(folder, RemovedMarkerFileName);
            foreach (var file in Directory.GetFiles(folder).Where(file => file != marker))
            {
                File.Delete(file);
            }

            foreach (var child in Directory.GetDirectories(folder).Where(child => !IsInstallFolder(child)))
            {
                Directory.Delete(child, recursive: true);
            }

            File.Delete(marker);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete the retired legacy plugin files in {Folder}", folder);
        }
    }
}
