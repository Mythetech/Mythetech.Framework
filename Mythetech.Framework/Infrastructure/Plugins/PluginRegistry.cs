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
}

