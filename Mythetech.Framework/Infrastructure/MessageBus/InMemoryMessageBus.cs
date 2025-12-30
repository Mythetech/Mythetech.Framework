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
    private readonly IEnumerable<IMessagePipe> _globalPipes;
    private readonly IEnumerable<IConsumerFilter> _filters;

    /// <summary>
    /// Constructor for the in memory implementation
    /// </summary>
    /// <param name="serviceProvider">Service provider for registration</param>
    /// <param name="logger">Logger</param>
    /// <param name="globalPipes">Global message pipes</param>
    /// <param name="filters">Consumer filters</param>
    public InMemoryMessageBus(
        IServiceProvider serviceProvider, 
        ILogger<InMemoryMessageBus> logger,
        IEnumerable<IMessagePipe> globalPipes,
        IEnumerable<IConsumerFilter> filters)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _globalPipes = globalPipes;
        _filters = filters;
    }

    /// <inheritdoc/>
    public Task PublishAsync<TMessage>(TMessage message) where TMessage : class
        => PublishAsync(message, new PublishConfiguration { Timeout = Timeout.InfiniteTimeSpan });

    /// <inheritdoc/>
    public async Task PublishAsync<TMessage>(TMessage message, PublishConfiguration configuration) where TMessage : class
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(configuration.CancellationToken);
        
        if (configuration.Timeout != Timeout.InfiniteTimeSpan)
        {
            linkedCts.CancelAfter(configuration.Timeout);
        }

        if (!await RunGlobalPipesAsync(message, linkedCts.Token))
            return;

        if (!await RunTypedPipesAsync(message, linkedCts.Token))
            return;

        var registeredConsumers = GetOrResolveConsumers<TMessage>();

        var manualSubscribers = _subscribers.TryGetValue(typeof(TMessage), out var subscribers)
            ? subscribers.Cast<IConsumer<TMessage>>()
            : [];

        var allConsumers = registeredConsumers.Concat(manualSubscribers);
        
        var filteredConsumers = allConsumers.Where(consumer => 
            _filters.All(filter => filter.ShouldInvoke(consumer, message)));

        var tasks = filteredConsumers.Select(async consumer =>
        {
            try
            {
                await consumer.Consume(message).WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Consumer {ConsumerType} timed out or was cancelled for message {MessageType}",
                    consumer.GetType().Name,
                    typeof(TMessage).Name);
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

    private async Task<bool> RunGlobalPipesAsync<TMessage>(TMessage message, CancellationToken cancellationToken) 
        where TMessage : class
    {
        foreach (var pipe in _globalPipes)
        {
            try
            {
                if (!await pipe.ProcessAsync(message, cancellationToken))
                {
                    _logger.LogDebug(
                        "Message {MessageType} blocked by global pipe {PipeType}",
                        typeof(TMessage).Name,
                        pipe.GetType().Name);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in global pipe {PipeType} for message {MessageType}",
                    pipe.GetType().Name,
                    typeof(TMessage).Name);
            }
        }
        return true;
    }

    private async Task<bool> RunTypedPipesAsync<TMessage>(TMessage message, CancellationToken cancellationToken) 
        where TMessage : class
    {
        var typedPipes = _serviceProvider.GetServices<IMessagePipe<TMessage>>();
        
        foreach (var pipe in typedPipes)
        {
            try
            {
                if (!await pipe.ProcessAsync(message, cancellationToken))
                {
                    _logger.LogDebug(
                        "Message {MessageType} blocked by typed pipe {PipeType}",
                        typeof(TMessage).Name,
                        pipe.GetType().Name);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in typed pipe {PipeType} for message {MessageType}",
                    pipe.GetType().Name,
                    typeof(TMessage).Name);
            }
        }
        return true;
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
