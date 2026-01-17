namespace Mythetech.Framework.Desktop.Updates;

/// <summary>
/// Configuration options for the update service.
/// </summary>
public class UpdateServiceOptions
{
    /// <summary>
    /// URL to the update feed (Azure Blob Storage, GitHub Releases, etc.).
    /// Required for update checks to work.
    /// </summary>
    public string? UpdateUrl { get; set; }

    /// <summary>
    /// Release channel (e.g., "stable", "beta", "preview").
    /// When null, uses the default channel.
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// Whether to allow downgrades (useful for switching channels).
    /// </summary>
    public bool AllowDowngrade { get; set; }
}
