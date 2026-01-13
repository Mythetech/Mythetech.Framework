using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Events;

namespace Mythetech.Framework.Infrastructure.Plugins.Consumers;

/// <summary>
/// Handles plugin settings changes and applies them to PluginState.
/// </summary>
public class PluginSettingsConsumer : IConsumer<SettingsModelChanged<PluginSettings>>
{
    private readonly PluginState _pluginState;
    private readonly ILogger<PluginSettingsConsumer> _logger;

    /// <summary>
    /// Creates a new plugin settings consumer.
    /// </summary>
    public PluginSettingsConsumer(
        PluginState pluginState,
        ILogger<PluginSettingsConsumer> logger)
    {
        _pluginState = pluginState;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Consume(SettingsModelChanged<PluginSettings> message)
    {
        var settings = message.Settings;

        // Apply PluginsActive toggle immediately
        if (_pluginState.PluginsActive != settings.PluginsActive)
        {
            _pluginState.PluginsActive = settings.PluginsActive;
            _logger.LogInformation("Plugin system {State}",
                settings.PluginsActive ? "activated" : "deactivated");
        }

        return Task.CompletedTask;
    }
}
