namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Central registry for all settings models in the application.
/// Manages registration, retrieval, and change notification for settings domains.
/// </summary>
public interface ISettingsProvider
{
    /// <summary>
    /// Gets all registered settings models, ordered by their Order property.
    /// </summary>
    IReadOnlyList<SettingsBase> GetAllSettings();

    /// <summary>
    /// Gets a specific settings model by type.
    /// </summary>
    /// <typeparam name="T">The settings type to retrieve.</typeparam>
    /// <returns>The settings instance, or null if not registered.</returns>
    T? GetSettings<T>() where T : SettingsBase;

    /// <summary>
    /// Gets a specific settings model by its ID.
    /// </summary>
    /// <param name="settingsId">The unique settings identifier.</param>
    /// <returns>The settings instance, or null if not registered.</returns>
    SettingsBase? GetSettingsById(string settingsId);

    /// <summary>
    /// Registers a settings model with the provider.
    /// </summary>
    /// <param name="settings">The settings instance to register.</param>
    void RegisterSettings(SettingsBase settings);

    /// <summary>
    /// Publishes a change notification for the specified settings model.
    /// Call this when settings are confirmed (e.g., dialog closed) or when
    /// immediate persistence is needed.
    /// </summary>
    /// <param name="settings">The settings instance that changed.</param>
    Task NotifySettingsChangedAsync(SettingsBase settings);

    /// <summary>
    /// Applies persisted settings data to registered models.
    /// Called during application startup.
    /// </summary>
    /// <param name="persistedData">Dictionary of settingsId to JSON data.</param>
    Task ApplyPersistedSettingsAsync(Dictionary<string, string> persistedData);

    /// <summary>
    /// Searches all registered settings for matching metadata.
    /// Searches across Label, Description, Group, and section DisplayName.
    /// </summary>
    /// <param name="searchTerm">The search term to match against.</param>
    /// <returns>Collection of search results.</returns>
    IEnumerable<SettingsSearchResult> SearchSettings(string searchTerm);
}
