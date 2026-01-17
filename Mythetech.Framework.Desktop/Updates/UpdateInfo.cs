namespace Mythetech.Framework.Desktop.Updates;

/// <summary>
/// Information about an available application update.
/// </summary>
public class UpdateInfo
{
    /// <summary>
    /// The new version available.
    /// </summary>
    public required Version TargetVersion { get; init; }

    /// <summary>
    /// Whether the update has been downloaded and is ready to install.
    /// </summary>
    public bool IsDownloaded { get; set; }

    /// <summary>
    /// Release notes or changelog, if available.
    /// </summary>
    public string? ReleaseNotes { get; init; }

    /// <summary>
    /// Size of the update in bytes.
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// Internal Velopack update object for applying the update.
    /// </summary>
    internal object? VelopackUpdate { get; set; }
}
