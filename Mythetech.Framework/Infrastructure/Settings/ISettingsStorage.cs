namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Interface for persisting and loading settings.
/// Implementations are platform-specific and provided by consuming apps
/// or optional framework platform packages.
///
/// IMPORTANT: Settings storage should be separate from plugin storage
/// (e.g., different .db file for app settings vs. plugin data).
/// </summary>
public interface ISettingsStorage
{
    /// <summary>
    /// Persists settings data for a specific settings domain.
    /// </summary>
    /// <param name="settingsId">The unique identifier for the settings domain.</param>
    /// <param name="jsonData">The serialized JSON data to persist.</param>
    Task SaveSettingsAsync(string settingsId, string jsonData);

    /// <summary>
    /// Loads persisted settings data for a specific settings domain.
    /// </summary>
    /// <param name="settingsId">The unique identifier for the settings domain.</param>
    /// <returns>The serialized JSON data, or null if not found.</returns>
    Task<string?> LoadSettingsAsync(string settingsId);

    /// <summary>
    /// Loads all persisted settings data.
    /// </summary>
    /// <returns>Dictionary mapping settingsId to serialized JSON data.</returns>
    Task<Dictionary<string, string>> LoadAllSettingsAsync();
}
