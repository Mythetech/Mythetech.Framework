using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Events;

namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Central registry for all settings models.
/// Subscribes to changes from each model and publishes them via MessageBus.
/// </summary>
public class SettingsProvider : ISettingsProvider
{
    private readonly IMessageBus _bus;
    private readonly ILogger<SettingsProvider> _logger;
    private readonly Dictionary<string, SettingsBase> _settings = new();

    /// <summary>
    /// Creates a new settings provider.
    /// </summary>
    /// <param name="bus">The message bus for publishing change events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public SettingsProvider(IMessageBus bus, ILogger<SettingsProvider> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<SettingsBase> GetAllSettings()
        => _settings.Values.OrderBy(s => s.Order).ThenBy(s => s.DisplayName).ToList().AsReadOnly();

    /// <inheritdoc />
    public T? GetSettings<T>() where T : SettingsBase
        => _settings.Values.OfType<T>().FirstOrDefault();

    /// <inheritdoc />
    public SettingsBase? GetSettingsById(string settingsId)
        => _settings.TryGetValue(settingsId, out var settings) ? settings : null;

    /// <inheritdoc />
    public void RegisterSettings(SettingsBase settings)
    {
        if (_settings.ContainsKey(settings.SettingsId))
        {
            _logger.LogWarning("Settings with ID {SettingsId} already registered, skipping", settings.SettingsId);
            return;
        }

        _settings[settings.SettingsId] = settings;
        _logger.LogDebug("Registered settings: {SettingsId} ({DisplayName})", settings.SettingsId, settings.DisplayName);
    }

    /// <summary>
    /// Publishes a change notification for the specified settings model.
    /// Call this when settings are confirmed (e.g., dialog closed) or when
    /// immediate persistence is needed.
    /// </summary>
    public async Task NotifySettingsChangedAsync(SettingsBase settings)
    {
        try
        {
            await _bus.PublishAsync(new SettingsModelChanged(settings));
            settings.ClearDirty();
            _logger.LogDebug("Published settings change for {SettingsId}", settings.SettingsId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish settings change for {SettingsId}", settings.SettingsId);
        }
    }

    /// <inheritdoc />
    public async Task ApplyPersistedSettingsAsync(Dictionary<string, string> persistedData)
    {
        foreach (var (settingsId, jsonData) in persistedData)
        {
            if (!_settings.TryGetValue(settingsId, out var settings))
            {
                _logger.LogDebug("No registered settings for persisted ID: {SettingsId}", settingsId);
                continue;
            }

            try
            {
                ApplyJsonToSettings(settings, jsonData);
                _logger.LogDebug("Applied persisted settings for {SettingsId}", settingsId);

                // Publish change event so consumers can apply the loaded settings
                await _bus.PublishAsync(new SettingsModelChanged(settings));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply persisted settings for {SettingsId}", settingsId);
            }
        }
    }

    private void ApplyJsonToSettings(SettingsBase settings, string jsonData)
    {
        using var document = JsonDocument.Parse(jsonData);
        var root = document.RootElement;
        var type = settings.GetType();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Only apply to properties with [Setting] attribute
            if (property.GetCustomAttribute<SettingAttribute>() == null)
                continue;

            if (!property.CanWrite)
                continue;

            if (!root.TryGetProperty(property.Name, out var jsonValue))
                continue;

            try
            {
                var value = JsonSerializer.Deserialize(jsonValue.GetRawText(), property.PropertyType);

                // Use reflection to set backing field directly to avoid triggering change events
                // during initial load. We'll publish one event after all properties are set.
                SetPropertyWithoutNotification(settings, property, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize setting {Property} for {SettingsId}",
                    property.Name, settings.SettingsId);
            }
        }
    }

    private static void SetPropertyWithoutNotification(SettingsBase settings, PropertyInfo property, object? value)
    {
        // Try to find and set the backing field directly
        // Guard against single-character property names (edge case but possible)
        var backingFieldName = property.Name.Length > 1
            ? $"_{char.ToLowerInvariant(property.Name[0])}{property.Name[1..]}"
            : $"_{char.ToLowerInvariant(property.Name[0])}";

        var backingField = settings.GetType().GetField(backingFieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (backingField != null)
        {
            backingField.SetValue(settings, value);
        }
        else
        {
            // Fallback to property setter (will trigger notification)
            property.SetValue(settings, value);
        }
    }
}
