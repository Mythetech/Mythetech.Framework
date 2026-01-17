namespace Mythetech.Framework.Desktop.Updates;

/// <summary>
/// Abstraction for application update operations.
/// Publishes events via message bus instead of using callbacks.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Whether the app is running in a Velopack-managed context (vs. debug/dev).
    /// </summary>
    bool IsInstalled { get; }

    /// <summary>
    /// Current application version.
    /// </summary>
    Version? CurrentVersion { get; }

    /// <summary>
    /// The currently available update, if any.
    /// </summary>
    UpdateInfo? AvailableUpdate { get; }

    /// <summary>
    /// Checks for available updates.
    /// Publishes UpdateCheckStarted, UpdateCheckCompleted, and UpdateAvailable events.
    /// </summary>
    Task CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the available update.
    /// Publishes UpdateDownloadStarted, UpdateDownloadProgress, and UpdateDownloadCompleted events.
    /// </summary>
    Task DownloadUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies downloaded update and restarts the application.
    /// </summary>
    void ApplyUpdateAndRestart();

    /// <summary>
    /// Applies downloaded update on next application exit (no immediate restart).
    /// </summary>
    void ApplyUpdateOnExit();
}
