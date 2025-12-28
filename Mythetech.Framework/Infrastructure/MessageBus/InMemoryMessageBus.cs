using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// In memory implementation of the generic bus to work in desktop + webassembly blazor applications
/// </summary>
public class InMemoryMessageBus : IMessageBus
{
    private readonly Dictionary<Type, List<Type>> _registeredConsumerTypes = new();
    private readonly Dictionary<Type, List<object>> _cachedConsumers = new();
    private readonly Dictionary<Type, List<object>> _subscribers = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryMessageBus> _logger;

    /// <summary>
    /// Constructor for the in memory implementation
    /// </summary>
    /// <param name="serviceProvider">Service provider for registration</param>
    /// <param name="logger">Logger</param>
    public InMemoryMessageBus(IServiceProvider serviceProvider, ILogger<InMemoryMessageBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TMessage>(TMessage message) where TMessage : class
    {
        var registeredConsumers = GetOrResolveConsumers<TMessage>();

        var manualSubscribers = _subscribers.TryGetValue(typeof(TMessage), out var subscribers)
            ? subscribers.Cast<IConsumer<TMessage>>()
            : [];

        var allConsumers = registeredConsumers.Concat(manualSubscribers);

        var tasks = allConsumers.Select(async consumer =>
        {
            try
            {
                await consumer.Consume(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error in message bus consumer {ConsumerType} handling message {MessageType}", 
                    consumer.GetType().Name,
                    typeof(TMessage).Name);
            }
        });
        
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public void RegisterConsumerType<TMessage, TConsumer>() where TMessage : class where TConsumer : IConsumer<TMessage>
    {
        if (!_registeredConsumerTypes.ContainsKey(typeof(TMessage)))
        {
            _registeredConsumerTypes[typeof(TMessage)] = new List<Type>();
        }

        _registeredConsumerTypes[typeof(TMessage)].Add(typeof(TConsumer));
    }

    private List<IConsumer<TMessage>> GetOrResolveConsumers<TMessage>() where TMessage : class
    {
        var messageType = typeof(TMessage);

        if (!_cachedConsumers.TryGetValue(messageType, out var cached))
        {
            if (!_registeredConsumerTypes.TryGetValue(messageType, out var consumerTypes))
                return [];

            cached = consumerTypes
                .Select(type => _serviceProvider.GetService(type))
                .OfType<object>()
                .ToList();

            _cachedConsumers[messageType] = cached;
        }

        return cached.Cast<IConsumer<TMessage>>().ToList();
    }

    /// <inheritdoc/>
    public void Subscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
        if (!_subscribers.ContainsKey(typeof(TMessage)))
            _subscribers[typeof(TMessage)] = [];

        _subscribers[typeof(TMessage)].Add(consumer);
    }

    /// <inheritdoc/>
    public void Unsubscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
        if (!_subscribers.TryGetValue(typeof(TMessage), out var handlers)) return;
        
        handlers.Remove(consumer);
        if (handlers.Count == 0)
            _subscribers.Remove(typeof(TMessage));
    }
}