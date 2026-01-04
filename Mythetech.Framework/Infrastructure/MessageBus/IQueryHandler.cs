namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// Handler for request/response queries. Returns a typed result.
/// Use this instead of <see cref="IConsumer{TMessage}"/> when the caller needs a response.
/// </summary>
/// <typeparam name="TMessage">The query message type</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler</typeparam>
public interface IQueryHandler<TMessage, TResponse>
    where TMessage : class
    where TResponse : class
{
    /// <summary>
    /// Handles the query and returns a response.
    /// </summary>
    /// <param name="message">The query message</param>
    /// <returns>The response from handling the query</returns>
    Task<TResponse> Handle(TMessage message);
}
