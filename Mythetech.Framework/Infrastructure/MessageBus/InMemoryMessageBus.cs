using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Telemetry;

namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// In memory implementation of the generic bus to work in desktop + webassembly blazor applications
/// </summary>
/// <remarks>
/// Publishing is on the hot path of every app, so it avoids per-publish allocations: consumer,
/// subscriber and pipe lists are cached as arrays that are replaced rather than mutated, no
/// cancellation source is created unless a timeout needs one, and activity names are only built
/// when a listener is attached.
/// </remarks>
public class InMemoryMessageBus : IMessageBus
{
    private static readonly PublishConfiguration UnboundedPublishConfiguration = new() { Timeout = Timeout.InfiniteTimeSpan };

    private readonly ConcurrentDictionary<Type, List<Type>> _registeredConsumerTypes = new();
    private readonly ConcurrentDictionary<Type, object> _cachedConsumers = new();
    private readonly ConcurrentDictionary<Type, object> _subscribers = new();
    private readonly ConcurrentDictionary<Type, (Type HandlerType, Type ResponseType)> _registeredQueryHandlerTypes = new();
    private readonly ConcurrentDictionary<Type, object> _cachedQueryHandlers = new();
    private readonly ConcurrentDictionary<Type, object> _cachedTypedPipes = new();
    private readonly Lock _subscribersLock = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryMessageBus> _logger;
    private readonly IMessagePipe[] _globalPipes;
    private readonly IConsumerFilter[] _filters;

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
        _globalPipes = globalPipes.ToArray();
        _filters = filters.ToArray();
    }

    /// <inheritdoc/>
    public Task PublishAsync<TMessage>(TMessage message) where TMessage : class
        => PublishAsync(message, UnboundedPublishConfiguration);

    /// <inheritdoc/>
    public async Task PublishAsync<TMessage>(TMessage message, PublishConfiguration configuration) where TMessage : class
    {
        using var activity = StartActivity(MessageNames<TMessage>.PublishActivity);
        activity?.SetTag(FrameworkTelemetry.Tags.MessageType, MessageNames<TMessage>.Name);

        using var timeoutCts = configuration.Timeout != Timeout.InfiniteTimeSpan
            ? CancellationTokenSource.CreateLinkedTokenSource(configuration.CancellationToken)
            : null;
        timeoutCts?.CancelAfter(configuration.Timeout);
        var cancellationToken = timeoutCts?.Token ?? configuration.CancellationToken;

        if (_globalPipes.Length != 0 && !await RunGlobalPipesAsync(message, cancellationToken))
        {
            activity?.SetTag(FrameworkTelemetry.Tags.Success, false);
            return;
        }

        var typedPipes = GetOrResolveTypedPipes<TMessage>();
        if (typedPipes.Length != 0 && !await RunTypedPipesAsync(typedPipes, message, cancellationToken))
        {
            activity?.SetTag(FrameworkTelemetry.Tags.Success, false);
            return;
        }

        var registeredConsumers = GetOrResolveConsumers<TMessage>();
        var subscribers = GetSubscribers<TMessage>();

        // Consumers that finish synchronously never need to be awaited, so the pending array is
        // only allocated once one of them actually suspends.
        Task[]? pending = null;
        var pendingCount = 0;
        var consumerCount = 0;

        foreach (var consumers in (ReadOnlySpan<IConsumer<TMessage>[]>)[registeredConsumers, subscribers])
        {
            foreach (var consumer in consumers)
            {
                if (!ShouldInvoke(consumer, message))
                    continue;

                consumerCount++;
                var task = ConsumeAsync(consumer, message, cancellationToken);
                if (task.IsCompleted)
                    continue;

                pending ??= new Task[registeredConsumers.Length + subscribers.Length];
                pending[pendingCount++] = task;
            }
        }

        activity?.SetTag(FrameworkTelemetry.Tags.ConsumerCount, consumerCount);

        if (pendingCount == 1)
        {
            await pending![0];
        }
        else if (pendingCount > 1)
        {
            await Task.WhenAll(pending!.AsSpan(0, pendingCount));
        }

        activity?.SetTag(FrameworkTelemetry.Tags.Success, true);
    }

    private bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message) where TMessage : class
    {
        foreach (var filter in _filters)
        {
            if (!filter.ShouldInvoke(consumer, message))
                return false;
        }
        return true;
    }

    // Never faults: every consumer failure is logged here, so PublishAsync can skip completed tasks.
    private async Task ConsumeAsync<TMessage>(IConsumer<TMessage> consumer, TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        using var consumerActivity = FrameworkTelemetry.MessageBusSource.HasListeners()
            ? FrameworkTelemetry.MessageBusSource.StartActivity($"Consume:{consumer.GetType().Name}")
            : null;
        consumerActivity?.SetTag(FrameworkTelemetry.Tags.ConsumerType, consumer.GetType().Name);
        consumerActivity?.SetTag(FrameworkTelemetry.Tags.MessageType, MessageNames<TMessage>.Name);

        try
        {
            await consumer.Consume(message).WaitAsync(cancellationToken);
            consumerActivity?.SetTag(FrameworkTelemetry.Tags.Success, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            consumerActivity?.SetTag(FrameworkTelemetry.Tags.Success, false);
            consumerActivity?.SetTag(FrameworkTelemetry.Tags.ErrorMessage, "Timed out or cancelled");
            _logger.LogWarning(
                "Consumer {ConsumerType} timed out or was cancelled for message {MessageType}",
                consumer.GetType().Name,
                MessageNames<TMessage>.Name);
        }
        catch (Exception ex)
        {
            consumerActivity?.SetTag(FrameworkTelemetry.Tags.Success, false);
            consumerActivity?.SetTag(FrameworkTelemetry.Tags.ErrorMessage, ex.Message);
            _logger.LogError(ex,
                "Error in message bus consumer {ConsumerType} handling message {MessageType}",
                consumer.GetType().Name,
                MessageNames<TMessage>.Name);
        }
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
                        MessageNames<TMessage>.Name,
                        pipe.GetType().Name);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in global pipe {PipeType} for message {MessageType}",
                    pipe.GetType().Name,
                    MessageNames<TMessage>.Name);
            }
        }
        return true;
    }

    private async Task<bool> RunTypedPipesAsync<TMessage>(IMessagePipe<TMessage>[] typedPipes, TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        foreach (var pipe in typedPipes)
        {
            try
            {
                if (!await pipe.ProcessAsync(message, cancellationToken))
                {
                    _logger.LogDebug(
                        "Message {MessageType} blocked by typed pipe {PipeType}",
                        MessageNames<TMessage>.Name,
                        pipe.GetType().Name);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in typed pipe {PipeType} for message {MessageType}",
                    pipe.GetType().Name,
                    MessageNames<TMessage>.Name);
            }
        }
        return true;
    }

    private IMessagePipe<TMessage>[] GetOrResolveTypedPipes<TMessage>() where TMessage : class
        => (IMessagePipe<TMessage>[])_cachedTypedPipes.GetOrAdd(typeof(TMessage), _ =>
            _serviceProvider.GetServices<IMessagePipe<TMessage>>().ToArray());

    /// <inheritdoc/>
    public void RegisterConsumerType<TMessage, TConsumer>() where TMessage : class where TConsumer : IConsumer<TMessage>
    {
        var consumerTypes = _registeredConsumerTypes.GetOrAdd(typeof(TMessage), _ => new List<Type>());
        lock (consumerTypes)
        {
            consumerTypes.Add(typeof(TConsumer));
        }
    }

    private IConsumer<TMessage>[] GetOrResolveConsumers<TMessage>() where TMessage : class
        => (IConsumer<TMessage>[])_cachedConsumers.GetOrAdd(typeof(TMessage), messageType =>
        {
            if (!_registeredConsumerTypes.TryGetValue(messageType, out var consumerTypes))
                return Array.Empty<IConsumer<TMessage>>();

            Type[] typesCopy;
            lock (consumerTypes)
            {
                typesCopy = consumerTypes.ToArray();
            }

            return typesCopy
                .Select(type => _serviceProvider.GetService(type))
                .OfType<IConsumer<TMessage>>()
                .ToArray();
        });

    private IConsumer<TMessage>[] GetSubscribers<TMessage>() where TMessage : class
        => _subscribers.TryGetValue(typeof(TMessage), out var subscribers)
            ? (IConsumer<TMessage>[])subscribers
            : [];

    // Subscriber arrays are replaced under the lock and never mutated, so PublishAsync reads them
    // without locking and a concurrent Unsubscribe cannot strand a new subscription.

    /// <inheritdoc/>
    public void Subscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
        lock (_subscribersLock)
        {
            _subscribers[typeof(TMessage)] = (IConsumer<TMessage>[])[.. GetSubscribers<TMessage>(), consumer];
        }
    }

    /// <inheritdoc/>
    public void Unsubscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
        lock (_subscribersLock)
        {
            var current = GetSubscribers<TMessage>();
            var index = Array.IndexOf(current, consumer);
            if (index < 0) return;

            if (current.Length == 1)
            {
                _subscribers.TryRemove(typeof(TMessage), out _);
                return;
            }

            _subscribers[typeof(TMessage)] = (IConsumer<TMessage>[])[.. current.AsSpan(0, index), .. current.AsSpan(index + 1)];
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
        var messageTypeName = MessageNames<TMessage>.Name;
        using var activity = StartActivity(MessageNames<TMessage>.QueryActivity);
        activity?.SetTag(FrameworkTelemetry.Tags.MessageType, messageTypeName);

        using var timeoutCts = configuration.Timeout != Timeout.InfiniteTimeSpan
            ? CancellationTokenSource.CreateLinkedTokenSource(configuration.CancellationToken)
            : null;
        timeoutCts?.CancelAfter(configuration.Timeout);
        var cancellationToken = timeoutCts?.Token ?? configuration.CancellationToken;

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
            var result = await handler.Handle(message).WaitAsync(cancellationToken);
            activity?.SetTag(FrameworkTelemetry.Tags.Success, true);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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

    private static Activity? StartActivity(string name)
        => FrameworkTelemetry.MessageBusSource.HasListeners()
            ? FrameworkTelemetry.MessageBusSource.StartActivity(name)
            : null;

    private static class MessageNames<TMessage>
    {
        public static readonly string Name = typeof(TMessage).Name;
        public static readonly string PublishActivity = $"Publish:{Name}";
        public static readonly string QueryActivity = $"Query:{Name}";
    }
}
