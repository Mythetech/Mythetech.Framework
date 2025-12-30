namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Provides persistent key-value storage for plugins.
/// Data is automatically isolated per-plugin.
/// </summary>
public interface IPluginStorage
{
    /// <summary>
    /// Get a value by key
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="key">The storage key</param>
    /// <returns>The value or default if not found</returns>
    Task<T?> GetAsync<T>(string key);
    
    /// <summary>
    /// Set a value by key. Creates or overwrites.
    /// </summary>
    /// <typeparam name="T">The type to serialize</typeparam>
    /// <param name="key">The storage key</param>
    /// <param name="value">The value to store</param>
    Task SetAsync<T>(string key, T value);
    
    /// <summary>
    /// Delete a value by key
    /// </summary>
    /// <param name="key">The storage key</param>
    /// <returns>True if the key existed and was deleted</returns>
    Task<bool> DeleteAsync(string key);
    
    /// <summary>
    /// Check if a key exists
    /// </summary>
    /// <param name="key">The storage key</param>
    Task<bool> ExistsAsync(string key);
    
    /// <summary>
    /// Get all keys, optionally filtered by prefix
    /// </summary>
    /// <param name="prefix">Optional prefix to filter keys</param>
    Task<IEnumerable<string>> GetKeysAsync(string? prefix = null);
    
    /// <summary>
    /// Clear all data for this plugin
    /// </summary>
    Task ClearAsync();
}

