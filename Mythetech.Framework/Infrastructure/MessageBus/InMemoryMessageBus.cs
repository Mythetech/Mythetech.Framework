using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Telemetry;

namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// In memory implementation of the generic bus to work in desktop + webassembly blazor applications
/// </summary>
public class InMemoryMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<Type, List<Type>> _registeredConsumerTypes = new();
    private readonly ConcurrentDictionary<Type, List<object>> _cachedConsumers = new();
    private readonly ConcurrentDictionary<Type, List<object>> _subscribers = new();
    private readonly ConcurrentDictionary<Type, (Type HandlerType, Type ResponseType)> _registeredQueryHandlerTypes = new();
    private readonly ConcurrentDictionary<Type, object> _cachedQueryHandlers = new();
    private readonly Lock _subscribersLock = new();

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
        var messageTypeName = typeof(TMessage).Name;
        using var activity = FrameworkTelemetry.MessageBusSource.StartActivity($"Publish:{messageTypeName}");
        activity?.SetTag(FrameworkTelemetry.Tags.MessageType, messageTypeName);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(configuration.CancellationToken);

        if (configuration.Timeout != Timeout.InfiniteTimeSpan)
        {
            linkedCts.CancelAfter(configuration.Timeout);
        }

        if (!await RunGlobalPipesAsync(message, linkedCts.Token))
        {
            activity?.SetTag(FrameworkTelemetry.Tags.Success, false);
            return;
        }

        if (!await RunTypedPipesAsync(message, linkedCts.Token))
        {
            activity?.SetTag(FrameworkTelemetry.Tags.Success, false);
            return;
        }

        var registeredConsumers = GetOrResolveConsumers<TMessage>();

        IEnumerable<IConsumer<TMessage>> manualSubscribers;
        if (_subscribers.TryGetValue(typeof(TMessage), out var subscribers))
        {
            lock (_subscribersLock)
            {
                manualSubscribers = subscribers.Cast<IConsumer<TMessage>>().ToList();
            }
        }
        else
        {
            manualSubscribers = [];
        }

        var allConsumers = registeredConsumers.Concat(manualSubscribers);

        var filteredConsumers = allConsumers.Where(consumer =>
            _filters.All(filter => filter.ShouldInvoke(consumer, message))).ToList();

        activity?.SetTag(FrameworkTelemetry.Tags.ConsumerCount, filteredConsumers.Count);

        var tasks = filteredConsumers.Select(async consumer =>
        {
            var consumerTypeName = consumer.GetType().Name;
            using var consumerActivity = FrameworkTelemetry.MessageBusSource.StartActivity($"Consume:{consumerTypeName}");
            consumerActivity?.SetTag(FrameworkTelemetry.Tags.ConsumerType, consumerTypeName);
            consumerActivity?.SetTag(FrameworkTelemetry.Tags.MessageType, messageTypeName);

            try
            {
                await consumer.Consume(message).WaitAsync(linkedCts.Token);
                consumerActivity?.SetTag(FrameworkTelemetry.Tags.Success, true);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
            {
                consumerActivity?.SetTag(FrameworkTelemetry.Tags.Success, false);
                consumerActivity?.SetTag(FrameworkTelemetry.Tags.ErrorMessage, "Timed out or cancelled");
                _logger.LogWarning(
                    "Consumer {ConsumerType} timed out or was cancelled for message {MessageType}",
                    consumerTypeName,
                    messageTypeName);
            }
            catch (Exception ex)
            {
                consumerActivity?.SetTag(FrameworkTelemetry.Tags.Success, false);
                consumerActivity?.SetTag(FrameworkTelemetry.Tags.ErrorMessage, ex.Message);
                _logger.LogError(ex,
                    "Error in message bus consumer {ConsumerType} handling message {MessageType}",
                    consumerTypeName,
                    messageTypeName);
            }
        });

        await Task.WhenAll(tasks);
        activity?.SetTag(FrameworkTelemetry.Tags.Success, true);
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
        var consumerTypes = _registeredConsumerTypes.GetOrAdd(typeof(TMessage), _ => new List<Type>());
        lock (consumerTypes)
        {
            consumerTypes.Add(typeof(TConsumer));
        }
    }

    private List<IConsumer<TMessage>> GetOrResolveConsumers<TMessage>() where TMessage : class
    {
        var messageType = typeof(TMessage);

        var cached = _cachedConsumers.GetOrAdd(messageType, _ =>
        {
            if (!_registeredConsumerTypes.TryGetValue(messageType, out var consumerTypes))
                return [];

            List<Type> typesCopy;
            lock (consumerTypes)
            {
                typesCopy = consumerTypes.ToList();
            }

            return typesCopy
                .Select(type => _serviceProvider.GetService(type))
                .OfType<object>()
                .ToList();
        });

        List<object> cachedCopy;
        lock (cached)
        {
            cachedCopy = cached.ToList();
        }

        return cachedCopy.Cast<IConsumer<TMessage>>().ToList();
    }

    /// <inheritdoc/>
    public void Subscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
        var subscribers = _subscribers.GetOrAdd(typeof(TMessage), _ => []);
        lock (_subscribersLock)
        {
            subscribers.Add(consumer);
        }
    }

    /// <inheritdoc/>
    public void Unsubscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
        if (!_subscribers.TryGetValue(typeof(TMessage), out var handlers)) return;

        lock (_subscribersLock)
        {
            handlers.Remove(consumer);
            if (handlers.Count == 0)
                _subscribers.TryRemove(typeof(TMessage), out _);
        }
    }

    /// <inheritdoc/>
    public Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message)
        where TMessage : class
        where TResponse : class
        => SendAsync<TMessage, TResponse>(message, new QueryConfiguration());

    /// <inheritdoc/>
    public async Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message, QueryConfiguration configuration)
        where TMessage : class
        where TResponse : class
    {
        var messageTypeName = typeof(TMessage).Name;
        using var activity = FrameworkTelemetry.MessageBusSource.StartActivity($"Query:{messageTypeName}");
        activity?.SetTag(FrameworkTelemetry.Tags.MessageType, messageTypeName);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(configuration.CancellationToken);

        if (configuration.Timeout != Timeout.InfiniteTimeSpan)
        {
            linkedCts.CancelAfter(configuration.Timeout);
        }

        var handler = GetOrResolveQueryHandler<TMessage, TResponse>();

        if (handler == null)
        {
            activity?.SetTag(FrameworkTelemetry.Tags.Success, false);
            activity?.SetTag(FrameworkTelemetry.Tags.ErrorMessage, "No handler registered");
            throw new InvalidOperationException(
                $"No query handler registered for message type {messageTypeName} with response type {typeof(TResponse).Name}");
        }

        var handlerTypeName = handler.GetType().Name;
        activity?.SetTag(FrameworkTelemetry.Tags.HandlerType, handlerTypeName);

        try
        {
            var result = await handler.Handle(message).WaitAsync(linkedCts.Token);
            activity?.SetTag(FrameworkTelemetry.Tags.Success, true);
            return result;
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            activity?.SetTag(FrameworkTelemetry.Tags.Success, false);
            activity?.SetTag(FrameworkTelemetry.Tags.ErrorMessage, "Timed out or cancelled");
            _logger.LogWarning(
                "Query handler {HandlerType} timed out or was cancelled for message {MessageType}",
                handlerTypeName,
                messageTypeName);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetTag(FrameworkTelemetry.Tags.Success, false);
            activity?.SetTag(FrameworkTelemetry.Tags.ErrorMessage, ex.Message);
            _logger.LogError(ex,
                "Error in query handler {HandlerType} handling message {MessageType}",
                handlerTypeName,
                messageTypeName);
            throw;
        }
    }

    /// <inheritdoc/>
    public void RegisterQueryHandler<TMessage, TResponse, THandler>()
        where TMessage : class
        where TResponse : class
        where THandler : IQueryHandler<TMessage, TResponse>
    {
        var messageType = typeof(TMessage);

        if (_registeredQueryHandlerTypes.ContainsKey(messageType))
        {
            _logger.LogWarning(
                "Query handler for message type {MessageType} is being overwritten. Previous: {PreviousHandler}, New: {NewHandler}",
                messageType.Name,
                _registeredQueryHandlerTypes[messageType].HandlerType.Name,
                typeof(THandler).Name);
        }

        _registeredQueryHandlerTypes[messageType] = (typeof(THandler), typeof(TResponse));
    }

    private IQueryHandler<TMessage, TResponse>? GetOrResolveQueryHandler<TMessage, TResponse>()
        where TMessage : class
        where TResponse : class
    {
        var messageType = typeof(TMessage);

        if (_cachedQueryHandlers.TryGetValue(messageType, out var cached))
        {
            return cached as IQueryHandler<TMessage, TResponse>;
        }

        if (!_registeredQueryHandlerTypes.TryGetValue(messageType, out var registration))
        {
            return null;
        }

        var handler = _serviceProvider.GetService(registration.HandlerType);

        if (handler != null)
        {
            _cachedQueryHandlers[messageType] = handler;
        }

        return handler as IQueryHandler<TMessage, TResponse>;
    }
}
