using Microsoft.AspNetCore.Components;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Plugins.Components;

/// <summary>
/// Base class for all plugin Blazor components.
/// Provides access to the plugin context, message bus, and shared state.
/// </summary>
public abstract class PluginComponentBase : ComponentBase, IDisposable
{
    /// <summary>
    /// The plugin context providing access to host services
    /// </summary>
    [Inject]
    protected PluginContext Context { get; set; } = default!;
    
    /// <summary>
    /// Shortcut to the message bus for publishing messages
    /// </summary>
    protected IMessageBus MessageBus => Context.MessageBus;
    
    /// <summary>
    /// Shortcut to the state store
    /// </summary>
    protected PluginStateStore StateStore => Context.StateStore;
    
    /// <summary>
    /// Publish a message with default plugin timeout (30s)
    /// </summary>
    protected Task PublishAsync<TMessage>(TMessage message) where TMessage : class
        => Context.PublishAsync(message);

    /// <summary>
    /// Get shared state for this plugin.
    /// State is automatically namespaced by the plugin's assembly.
    /// </summary>
    /// <typeparam name="T">The state type</typeparam>
    /// <param name="key">Optional key for multiple state values (default: "default")</param>
    protected T? GetState<T>(string key = "default")
        => StateStore.GetForPlugin<T>(GetType(), key);

    /// <summary>
    /// Set shared state for this plugin.
    /// State is automatically namespaced by the plugin's assembly.
    /// Triggers StateChanged event so other components can react.
    /// </summary>
    /// <typeparam name="T">The state type</typeparam>
    /// <param name="value">The value to store</param>
    /// <param name="key">Optional key for multiple state values (default: "default")</param>
    protected void SetState<T>(T value, string key = "default")
        => StateStore.SetForPlugin(GetType(), key, value);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        StateStore.StateChanged += OnPluginStateChanged;
    }

    private void OnPluginStateChanged(object? sender, PluginStateChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        StateStore.StateChanged -= OnPluginStateChanged;
        GC.SuppressFinalize(this);
    }
}
