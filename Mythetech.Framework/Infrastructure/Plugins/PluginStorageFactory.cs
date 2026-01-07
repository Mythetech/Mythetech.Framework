namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Factory for creating plugin-scoped storage instances.
/// Implementations are platform-specific (Desktop uses LiteDB, WebAssembly uses localStorage).
/// </summary>
public interface IPluginStorageFactory
{
    /// <summary>
    /// Create a storage instance scoped to a specific plugin.
    /// Returns null if storage is unavailable (e.g., permission issues).
    /// </summary>
    /// <param name="pluginId">The plugin's unique identifier</param>
    IPluginStorage? CreateForPlugin(string pluginId);
    
    /// <summary>
    /// Export all data for a plugin as a JSON string
    /// </summary>
    /// <param name="pluginId">The plugin's unique identifier</param>
    Task<string> ExportPluginDataAsync(string pluginId);
    
    /// <summary>
    /// Import data for a plugin from a JSON string
    /// </summary>
    /// <param name="pluginId">The plugin's unique identifier</param>
    /// <param name="jsonData">The JSON data to import</param>
    Task ImportPluginDataAsync(string pluginId, string jsonData);
    
    /// <summary>
    /// Delete all data for a plugin
    /// </summary>
    /// <param name="pluginId">The plugin's unique identifier</param>
    Task DeletePluginDataAsync(string pluginId);
}

