using System.Reflection;
using System.Runtime.Loader;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Runtime representation of a loaded plugin with its state and discovered components
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// The plugin's manifest containing metadata
    /// </summary>
    public required IPluginManifest Manifest { get; init; }
    
    /// <summary>
    /// The loaded assembly containing the plugin
    /// </summary>
    public required Assembly Assembly { get; init; }
    
    /// <summary>
    /// Whether this plugin is currently enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// When the plugin was loaded
    /// </summary>
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Discovered menu components from this plugin (types only)
    /// </summary>
    public IReadOnlyList<Type> MenuComponents { get; init; } = [];
    
    /// <summary>
    /// Discovered context panel components from this plugin (types only)
    /// </summary>
    public IReadOnlyList<Type> ContextPanelComponents { get; init; } = [];
    
    /// <summary>
    /// Metadata for menu components (with Icon, Title, Order pre-extracted)
    /// </summary>
    public IReadOnlyList<PluginComponentMetadata> MenuComponentsMetadata { get; init; } = [];
    
    /// <summary>
    /// Metadata for context panel components (with Icon, Title, Order pre-extracted)
    /// </summary>
    public IReadOnlyList<PluginComponentMetadata> ContextPanelComponentsMetadata { get; init; } = [];
    
    /// <summary>
    /// Path to the plugin DLL if loaded from disk
    /// </summary>
    public string? SourcePath { get; init; }

    /// <summary>
    /// The AssemblyLoadContext used to load this plugin.
    /// When set, allows the plugin's assembly to be unloaded when the plugin is removed.
    /// </summary>
    public AssemblyLoadContext? LoadContext { get; init; }
    
    /// <summary>
    /// Parse a version string (e.g., "1.0.0") to a Version object
    /// </summary>
    /// <param name="versionString">Version string in x.y.z format</param>
    /// <returns>Parsed Version, or null if parsing fails</returns>
    public static Version? ParseVersion(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            return null;
            
        if (Version.TryParse(versionString, out var version))
            return version;
            
        return null;
    }
    
    /// <summary>
    /// Compare this plugin's version with another version
    /// </summary>
    /// <param name="otherVersion">Version to compare against</param>
    /// <returns>Negative if this version is older, 0 if same, positive if newer</returns>
    public int CompareVersion(Version otherVersion)
    {
        ArgumentNullException.ThrowIfNull(otherVersion);
        return Manifest.Version.CompareTo(otherVersion);
    }
    
    /// <summary>
    /// Check if this plugin's version is newer than the specified version
    /// </summary>
    /// <param name="otherVersion">Version to compare against</param>
    /// <returns>True if this version is newer</returns>
    public bool IsNewerThan(Version otherVersion)
    {
        return CompareVersion(otherVersion) > 0;
    }
    
    /// <summary>
    /// Check if this plugin's version is the same as the specified version
    /// </summary>
    /// <param name="otherVersion">Version to compare against</param>
    /// <returns>True if versions are the same</returns>
    public bool IsSameVersion(Version otherVersion)
    {
        return CompareVersion(otherVersion) == 0;
    }
    
    /// <summary>
    /// Check if this plugin's version is older than the specified version
    /// </summary>
    /// <param name="otherVersion">Version to compare against</param>
    /// <returns>True if this version is older</returns>
    public bool IsOlderThan(Version otherVersion)
    {
        return CompareVersion(otherVersion) < 0;
    }
}

