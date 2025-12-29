namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// Filters which consumers should receive a message.
/// Applied per-consumer before invocation.
/// </summary>
public interface IConsumerFilter
{
    /// <summary>
    /// Determines if a consumer should receive the message.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="consumer">The consumer being evaluated</param>
    /// <param name="message">The message being published</param>
    /// <returns>True to invoke the consumer, false to skip it</returns>
    bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message) 
        where TMessage : class;
}

