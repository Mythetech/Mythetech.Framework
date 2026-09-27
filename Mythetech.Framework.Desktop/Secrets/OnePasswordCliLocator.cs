using System.Runtime.InteropServices;

namespace Mythetech.Framework.Desktop.Secrets;

/// <summary>
/// Finds the 1Password CLI executable. A macOS app launched from Finder, or a Linux app started from a desktop
/// launcher, gets a minimal PATH without Homebrew or /usr/local/bin, so starting "op" by name fails there even
/// when it is installed. PATH is searched first so a user's own install wins, then the usual install locations.
/// </summary>
internal static class OnePasswordCliLocator
{
    /// <summary>
    /// The bare command, returned when op is not found so starting it fails with the usual "not found" error.
    /// </summary>
    internal const string Command = "op";

    // Apple silicon Homebrew, then /usr/local/bin, which is Intel Homebrew and the documented .pkg and manual
    // install location.
    private static readonly string[] MacOSInstallLocations = ["/opt/homebrew/bin/op", "/usr/local/bin/op"];

    // The apt and yum packages install to /usr/bin; the documented manual install moves op to /usr/local/bin.
    private static readonly string[] LinuxInstallLocations = ["/usr/bin/op", "/usr/local/bin/op"];

    // The folder 1Password's documented manual install creates. winget and Scoop put op on the user's PATH,
    // which apps started from Explorer inherit, so PATH already covers them.
    private const string WindowsInstallFolder = "1Password CLI";

    /// <summary>
    /// Locates op for the current system.
    /// </summary>
    public static string Locate() => Locate(
        CurrentPlatform(),
        System.Environment.GetEnvironmentVariable("PATH"),
        File.Exists,
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles));

    /// <summary>
    /// Locates op from the given environment.
    /// </summary>
    /// <param name="platform">The platform whose executable name, PATH format and install locations apply.</param>
    /// <param name="pathVariable">The PATH value to search, or null when PATH is not set.</param>
    /// <param name="fileExists">Checks whether a file exists at a full path.</param>
    /// <param name="programFilesDirectory">The Program Files folder, used on Windows only.</param>
    /// <returns>The full path of op, or <see cref="Command"/> when it is not found.</returns>
    public static string Locate(
        OSPlatform platform,
        string? pathVariable,
        Func<string, bool> fileExists,
        string? programFilesDirectory = null)
    {
        var isWindows = platform == OSPlatform.Windows;
        var executableName = isWindows ? "op.exe" : Command;

        foreach (var directory in SplitPath(pathVariable, isWindows))
        {
            var candidate = Join(directory, executableName, isWindows);
            if (fileExists(candidate))
            {
                return candidate;
            }
        }

        foreach (var candidate in InstallLocations(platform, programFilesDirectory))
        {
            if (fileExists(candidate))
            {
                return candidate;
            }
        }

        return Command;
    }

    private static IEnumerable<string> InstallLocations(OSPlatform platform, string? programFilesDirectory)
    {
        if (platform == OSPlatform.OSX)
        {
            return MacOSInstallLocations;
        }

        if (platform == OSPlatform.Windows)
        {
            return string.IsNullOrWhiteSpace(programFilesDirectory)
                ? []
                : [Join(Join(programFilesDirectory, WindowsInstallFolder, isWindows: true), "op.exe", isWindows: true)];
        }

        return LinuxInstallLocations;
    }

    // Relative and empty entries are skipped: they would resolve against the app's working directory, which is
    // not a place an executable handed secrets should come from.
    private static IEnumerable<string> SplitPath(string? pathVariable, bool isWindows)
    {
        if (string.IsNullOrEmpty(pathVariable))
        {
            yield break;
        }

        foreach (var entry in pathVariable.Split(isWindows ? ';' : ':'))
        {
            var directory = isWindows ? entry.Trim().Trim('"') : entry;
            if (IsAbsolute(directory, isWindows))
            {
                yield return directory;
            }
        }
    }

    private static bool IsAbsolute(string directory, bool isWindows)
    {
        if (!isWindows)
        {
            return directory.StartsWith('/');
        }

        var isDrivePath = directory.Length >= 3
                          && char.IsAsciiLetter(directory[0])
                          && directory[1] == ':'
                          && directory[2] is '\\' or '/';
        return isDrivePath || directory.StartsWith(@"\\", StringComparison.Ordinal);
    }

    // Joined by hand rather than with Path.Combine so the result follows the target platform's separator
    // whatever platform the code runs on.
    private static string Join(string directory, string fileName, bool isWindows)
    {
        var separator = isWindows ? '\\' : '/';
        var trimmed = isWindows ? directory.TrimEnd('\\', '/') : directory.TrimEnd('/');
        return $"{trimmed}{separator}{fileName}";
    }

    private static OSPlatform CurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return OSPlatform.Windows;
        }

        return OperatingSystem.IsMacOS() ? OSPlatform.OSX : OSPlatform.Linux;
    }
}
