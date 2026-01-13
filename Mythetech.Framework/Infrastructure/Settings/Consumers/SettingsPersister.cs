using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Events;

namespace Mythetech.Framework.Infrastructure.Settings.Consumers;

/// <summary>
/// Auto-persists settings changes to storage.
/// Subscribes to SettingsModelChanged events and serializes the settings to JSON.
///
/// This consumer is optional - if no ISettingsStorage is registered, it will log
/// and skip persistence. This allows apps to opt-in to persistence.
/// </summary>
public class SettingsPersister : IConsumer<SettingsModelChanged>
{
    private readonly ISettingsStorage? _storage;
    private readonly ILogger<SettingsPersister> _logger;

    /// <summary>
    /// Creates a new persister instance.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="storage">Optional settings storage. If null, persistence is skipped.</param>
    public SettingsPersister(ILogger<SettingsPersister> logger, ISettingsStorage? storage = null)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(SettingsModelChanged message)
    {
        if (_storage == null)
        {
            _logger.LogDebug("No settings storage configured, skipping persistence for {SettingsId}",
                message.Settings.SettingsId);
            return;
        }

        var settings = message.Settings;

        try
        {
            var jsonData = SerializeSettings(settings);
            await _storage.SaveSettingsAsync(settings.SettingsId, jsonData);
            _logger.LogDebug("Persisted settings for {SettingsId}", settings.SettingsId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist settings for {SettingsId}", settings.SettingsId);
        }
    }

    private static string SerializeSettings(SettingsBase settings)
    {
        // Only serialize properties with [Setting] attribute
        var properties = settings.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<SettingAttribute>() != null && p.CanRead);

        var dict = new Dictionary<string, object?>();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(settings);
            dict[prop.Name] = value;
        }

        return JsonSerializer.Serialize(dict, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
