namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Classifies the paths plugins give for their assets and modules.
/// </summary>
internal static class PluginUrls
{
    /// <summary>
    /// Whether the path is an absolute or protocol-relative URL rather than a path inside the plugin.
    /// </summary>
    internal static bool IsExternalUrl(string path) =>
        path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("//", StringComparison.Ordinal);
}
