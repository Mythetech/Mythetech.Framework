using System.Text.Json.Serialization;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Plugin registry containing available plugins
/// </summary>
public class PluginRegistry
{
    /// <summary>
    /// List of available plugins in the registry
    /// </summary>
    [JsonPropertyName("plugins")]
    public List<RegistryPluginEntry> Plugins { get; set; } = [];
}

/// <summary>
/// Entry for a plugin in the registry
/// </summary>
public class RegistryPluginEntry
{
    /// <summary>
    /// Plugin ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Plugin name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Plugin version as string (e.g., "1.0.0")
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// URI to download the plugin package
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Supported platforms for this plugin (e.g., ["desktop", "webassembly"]).
    /// If null or empty, supports all platforms.
    /// </summary>
    [JsonPropertyName("supportedPlatforms")]
    public string[]? SupportedPlatforms { get; set; }

    /// <summary>
    /// If true, this plugin is only visible in non-production environments.
    /// </summary>
    [JsonPropertyName("isDevPlugin")]
    public bool IsDevPlugin { get; set; }

    /// <summary>
    /// If true, this plugin is a preview and only shown when the user opts in.
    /// </summary>
    [JsonPropertyName("isPreview")]
    public bool IsPreview { get; set; }

    /// <summary>
    /// Gets the parsed Platform values from SupportedPlatforms.
    /// Returns null if all platforms are supported.
    /// </summary>
    [JsonIgnore]
    public Platform[]? ParsedPlatforms
    {
        get
        {
            if (SupportedPlatforms is null || SupportedPlatforms.Length == 0)
                return null;

            var platforms = new List<Platform>();
            foreach (var platformStr in SupportedPlatforms)
            {
                if (Enum.TryParse<Platform>(platformStr, ignoreCase: true, out var platform))
                {
                    platforms.Add(platform);
                }
            }
            return platforms.Count > 0 ? platforms.ToArray() : null;
        }
    }

    /// <summary>
    /// Checks if this plugin supports the specified platform.
    /// Returns true if SupportedPlatforms is null/empty (supports all).
    /// </summary>
    public bool SupportsPlatform(Platform platform)
    {
        var parsed = ParsedPlatforms;
        return parsed is null || parsed.Contains(platform);
    }
}

