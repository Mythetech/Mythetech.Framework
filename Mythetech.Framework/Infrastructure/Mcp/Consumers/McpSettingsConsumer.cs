using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Infrastructure.Mcp.Server;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Events;

namespace Mythetech.Framework.Infrastructure.Mcp.Consumers;

/// <summary>
/// Handles MCP settings changes and applies them to the server configuration.
/// </summary>
public class McpSettingsConsumer : IConsumer<SettingsModelChanged<McpSettings>>
{
    private readonly McpServerState _serverState;
    private readonly McpServerOptions _options;
    private readonly ILogger<McpSettingsConsumer> _logger;

    /// <summary>
    /// Creates a new MCP settings consumer.
    /// </summary>
    public McpSettingsConsumer(
        McpServerState serverState,
        IOptions<McpServerOptions> options,
        ILogger<McpSettingsConsumer> logger)
    {
        _serverState = serverState;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Consume(SettingsModelChanged<McpSettings> message)
    {
        var settings = message.Settings;

        // Apply tool timeout immediately (no restart required)
        var newTimeout = TimeSpan.FromSeconds(settings.ToolTimeoutSeconds);
        if (_options.ToolTimeout != newTimeout)
        {
            _options.ToolTimeout = newTimeout;
            _logger.LogInformation("Applied MCP tool timeout: {Timeout}s", settings.ToolTimeoutSeconds);
        }

        // HTTP settings require restart - log warning if server is running
        if (_serverState.IsRunning)
        {
            if (_options.HttpEnabled != settings.HttpEnabled || _options.HttpPort != settings.HttpPort)
            {
                _logger.LogWarning(
                    "MCP HTTP settings changed while server is running. " +
                    "Restart the server for changes to take effect.");
            }
        }

        // Store for next startup
        _options.HttpEnabled = settings.HttpEnabled;
        _options.HttpPort = settings.HttpPort;

        return Task.CompletedTask;
    }
}
