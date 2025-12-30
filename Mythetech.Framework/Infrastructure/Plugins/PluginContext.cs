using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Provides plugins with access to host services and capabilities.
/// Injected into plugin components via PluginComponentBase.
/// </summary>
public class PluginContext
{
    /// <summary>
    /// Access to the message bus for publishing and subscribing to messages
    /// </summary>
    public required IMessageBus MessageBus { get; init; }
    
    /// <summary>
    /// Service provider for resolving additional services
    /// </summary>
    public required IServiceProvider Services { get; init; }
    
    /// <summary>
    /// Shared state storage for plugins (in-memory, not persistent)
    /// </summary>
    public required PluginStateStore StateStore { get; init; }
    
    /// <summary>
    /// Factory for creating persistent storage instances.
    /// May be null if the host doesn't support storage.
    /// </summary>
    public IPluginStorageFactory? StorageFactory { get; init; }
    
    /// <summary>
    /// Link opening service for opening URLs in the default browser
    /// </summary>
    public ILinkOpenService? LinkOpenService { get; init; }
    
    /// <summary>
    /// File save service for saving files
    /// </summary>
    public IFileSaveService? FileSaveService { get; init; }
    
    /// <summary>
    /// Information about the current plugin (set per-component)
    /// </summary>
    public PluginInfo? CurrentPlugin { get; set; }
    
    /// <summary>
    /// Publish a message with default plugin timeout (30s) to prevent runaway consumers
    /// </summary>
    public Task PublishAsync<TMessage>(TMessage message) where TMessage : class
        => MessageBus.PublishAsync(message, PublishConfiguration.Default);
    
    /// <summary>
    /// Publish a message with custom configuration
    /// </summary>
    public Task PublishAsync<TMessage>(TMessage message, PublishConfiguration configuration) where TMessage : class
        => MessageBus.PublishAsync(message, configuration);
}
