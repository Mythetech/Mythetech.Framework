namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// A pipe that processes all messages before they reach consumers.
/// Can be used for logging, validation, transformation, or blocking messages.
/// </summary>
public interface IMessagePipe
{
    /// <summary>
    /// Process the message before it reaches consumers.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="message">The message being published</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True to continue processing, false to block the message from reaching consumers</returns>
    Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken) 
        where TMessage : class;
}

/// <summary>
/// A pipe that processes messages of a specific type before they reach consumers.
/// Registered for a specific TMessage type.
/// </summary>
/// <typeparam name="TMessage">The message type this pipe handles</typeparam>
public interface IMessagePipe<TMessage> where TMessage : class
{
    /// <summary>
    /// Process the message before it reaches consumers.
    /// </summary>
    /// <param name="message">The message being published</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True to continue processing, false to block the message from reaching consumers</returns>
    Task<bool> ProcessAsync(TMessage message, CancellationToken cancellationToken);
}

