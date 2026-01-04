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
    /// Publishes a strongly typed message with configuration for timeout and cancellation.
    /// Use this overload when publishing from plugins or when timeout control is needed.
    /// </summary>
    /// <param name="message">The message to publish</param>
    /// <param name="configuration">Configuration specifying timeout and cancellation options</param>
    Task PublishAsync<TMessage>(TMessage message, PublishConfiguration configuration) where TMessage : class;
    
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

    /// <summary>
    /// Sends a query message and waits for a response from the registered handler.
    /// Unlike PublishAsync, this expects exactly one handler and returns a result.
    /// </summary>
    /// <typeparam name="TMessage">The query message type</typeparam>
    /// <typeparam name="TResponse">The expected response type</typeparam>
    /// <param name="message">The query message to send</param>
    /// <returns>The response from the handler</returns>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the query</exception>
    Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message)
        where TMessage : class
        where TResponse : class;

    /// <summary>
    /// Sends a query message with configuration for timeout and cancellation.
    /// </summary>
    /// <typeparam name="TMessage">The query message type</typeparam>
    /// <typeparam name="TResponse">The expected response type</typeparam>
    /// <param name="message">The query message to send</param>
    /// <param name="configuration">Configuration specifying timeout and cancellation options</param>
    /// <returns>The response from the handler</returns>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the query</exception>
    Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message, QueryConfiguration configuration)
        where TMessage : class
        where TResponse : class;

    /// <summary>
    /// Registers a query handler for the specified message and response types.
    /// Only one handler can be registered per message type.
    /// </summary>
    /// <typeparam name="TMessage">The query message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <typeparam name="THandler">The handler type implementing <see cref="IQueryHandler{TMessage, TResponse}"/></typeparam>
    void RegisterQueryHandler<TMessage, TResponse, THandler>()
        where TMessage : class
        where TResponse : class
        where THandler : IQueryHandler<TMessage, TResponse>;
}