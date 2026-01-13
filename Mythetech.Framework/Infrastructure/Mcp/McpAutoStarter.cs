using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Handles auto-starting the MCP server based on settings.
/// Call from UseMcp() after settings are loaded.
/// </summary>
public static class McpAutoStarter
{
    /// <summary>
    /// Auto-starts the MCP server if the AutoStart setting is enabled.
    /// Should be called after settings have been loaded from storage.
    /// </summary>
    /// <param name="services">The service provider.</param>
    public static async Task AutoStartIfConfiguredAsync(IServiceProvider services)
    {
        var logger = services.GetService<ILogger<McpServerState>>();
        var settingsProvider = services.GetService<ISettingsProvider>();
        var mcpSettings = settingsProvider?.GetSettings<McpSettings>();

        if (mcpSettings?.AutoStart == true)
        {
            var serverState = services.GetService<McpServerState>();
            if (serverState != null)
            {
                logger?.LogInformation("Auto-starting MCP server based on settings");
                await serverState.StartAsync();
            }
        }
    }
}
