namespace Mythetech.Components.Infrastructure.MessageBus;

/// <summary>
/// Base abstraction for receiving messages from the generic bus
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IConsumer<TMessage>
{
    /// <summary>
    /// Accept a typed message
    /// </summary>
    /// <param name="message">Concrete message type</param>
    Task Consume(TMessage message);
}