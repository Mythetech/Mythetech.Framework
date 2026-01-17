namespace Mythetech.Framework.Desktop.Updates.Events;

/// <summary>
/// Published when an update check starts.
/// </summary>
public record UpdateCheckStarted;

/// <summary>
/// Published when an update check completes.
/// </summary>
/// <param name="Update">The available update, or null if no update is available.</param>
public record UpdateCheckCompleted(UpdateInfo? Update);

/// <summary>
/// Published when an update is available.
/// </summary>
/// <param name="Update">The available update.</param>
public record UpdateAvailable(UpdateInfo Update);

/// <summary>
/// Published when update download starts.
/// </summary>
/// <param name="Update">The update being downloaded.</param>
public record UpdateDownloadStarted(UpdateInfo Update);

/// <summary>
/// Published during download with progress updates.
/// </summary>
/// <param name="Update">The update being downloaded.</param>
/// <param name="Progress">Download progress (0-100).</param>
public record UpdateDownloadProgress(UpdateInfo Update, int Progress);

/// <summary>
/// Published when download completes.
/// </summary>
/// <param name="Update">The downloaded update.</param>
public record UpdateDownloadCompleted(UpdateInfo Update);

/// <summary>
/// Published when update is ready to install.
/// </summary>
/// <param name="Update">The update ready to install.</param>
public record UpdateReadyToInstall(UpdateInfo Update);
