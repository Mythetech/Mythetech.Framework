using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// A pipe that logs all messages passing through the bus.
/// Register via services.AddMessageLogging() for optional message visibility.
/// </summary>
public class MessageLoggingPipe : IMessagePipe
{
    private readonly ILogger<MessageLoggingPipe> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public MessageLoggingPipe(ILogger<MessageLoggingPipe> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken) 
        where TMessage : class
    {
        _logger.LogInformation(
            "MessageBus: Publishing {MessageType} - {Message}",
            typeof(TMessage).Name,
            message);
        
        return Task.FromResult(true);
    }
}

