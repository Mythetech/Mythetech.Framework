namespace Mythetech.Framework.Infrastructure.Platform;

/// <summary>
/// Utility methods for detecting platform information from user agent strings.
/// </summary>
public static class PlatformInfo
{
    /// <summary>
    /// Checks if the user agent indicates a Mac platform.
    /// </summary>
    /// <param name="userAgent">The user agent string from the browser</param>
    /// <returns>True if the platform is Mac, false otherwise</returns>
    public static bool IsMac(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return false;
        return userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the user agent indicates a Windows platform.
    /// </summary>
    /// <param name="userAgent">The user agent string from the browser</param>
    /// <returns>True if the platform is Windows, false otherwise</returns>
    public static bool IsWindows(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return false;
        return userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the user agent indicates a Linux platform.
    /// </summary>
    /// <param name="userAgent">The user agent string from the browser</param>
    /// <returns>True if the platform is Linux, false otherwise</returns>
    public static bool IsLinux(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return false;
        return userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) &&
               !userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the appropriate meta key symbol for the platform.
    /// Returns Command symbol (⌘) for Mac, Windows key symbol (⊞) for Windows/other.
    /// </summary>
    /// <param name="userAgent">The user agent string from the browser</param>
    /// <returns>The platform-specific meta key symbol</returns>
    public static string GetMetaKeySymbol(string? userAgent)
    {
        return IsMac(userAgent) ? "⌘" : "⊞";
    }

    /// <summary>
    /// Gets a human-readable meta key name for the platform.
    /// Returns "Cmd" for Mac, "Ctrl" for Windows/other.
    /// </summary>
    /// <param name="userAgent">The user agent string from the browser</param>
    /// <returns>The platform-specific meta key name</returns>
    public static string GetMetaKeyName(string? userAgent)
    {
        return IsMac(userAgent) ? "Cmd" : "Ctrl";
    }
}
