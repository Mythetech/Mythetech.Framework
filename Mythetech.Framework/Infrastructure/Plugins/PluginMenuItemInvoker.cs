using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Commands;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Runs a plugin menu item's click handler so that a failure is logged and shown as a notification instead of escaping.
/// </summary>
public class PluginMenuItemInvoker(IMessageBus messageBus, ILogger<PluginMenuItemInvoker> logger)
{
    /// <summary>
    /// Runs <paramref name="onClick"/> and returns whether it completed without throwing.
    /// </summary>
    public async Task<bool> InvokeAsync(Func<PluginContext, Task> onClick, PluginContext context, string? pluginName = null)
    {
        try
        {
            await onClick(context);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Plugin menu item failed for plugin {PluginName}", pluginName ?? "(unknown)");
            await messageBus.PublishAsync(new AddNotification(
                pluginName is null ? "A plugin couldn't complete that action." : $"{pluginName} couldn't complete that action.",
                Severity.Error));
            return false;
        }
    }
}
