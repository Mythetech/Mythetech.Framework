using System.Reflection;

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
    /// Discovered menu components from this plugin
    /// </summary>
    public IReadOnlyList<Type> MenuComponents { get; init; } = [];
    
    /// <summary>
    /// Discovered context panel components from this plugin
    /// </summary>
    public IReadOnlyList<Type> ContextPanelComponents { get; init; } = [];
    
    /// <summary>
    /// Path to the plugin DLL if loaded from disk
    /// </summary>
    public string? SourcePath { get; init; }
}

