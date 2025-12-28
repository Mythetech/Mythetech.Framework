namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// An abstraction over a way to send messages from a component or service to other components and services.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a strongly typed message for other components and services.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message) where TMessage : class;
    
    /// <summary>
    /// Registers a concrete consumer for a given message type to the bus pipeline
    /// </summary>
    void RegisterConsumerType<TMessage, TConsumer>() where TMessage : class where TConsumer : IConsumer<TMessage>;
    
    /// <summary>
    /// Subscribes an IConsumer of TMessage to the bus.
    /// </summary>
    void Subscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class;
    
    /// <summary>
    /// Removes an IConsumer of TMessage subscription from the bus.
    /// </summary>
    void Unsubscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class;
}