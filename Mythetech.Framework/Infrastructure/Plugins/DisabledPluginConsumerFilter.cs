using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Consumer filter that blocks consumers from disabled plugins.
/// Checks if a consumer's assembly belongs to a disabled or globally inactive plugin.
/// </summary>
public class DisabledPluginConsumerFilter : IConsumerFilter
{
    private readonly PluginState _pluginState;

    /// <summary>
    /// Constructor
    /// </summary>
    public DisabledPluginConsumerFilter(PluginState pluginState)
    {
        _pluginState = pluginState;
    }

    /// <inheritdoc />
    public bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message) 
        where TMessage : class
    {
        var consumerAssembly = consumer.GetType().Assembly;
        
        var plugin = _pluginState.Plugins.FirstOrDefault(p => p.Assembly == consumerAssembly);
        
        if (plugin is null)
            return true;
        
        return plugin.IsEnabled && _pluginState.PluginsActive;
    }
}

