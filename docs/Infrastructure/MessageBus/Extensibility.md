# Extensibility

The MessageBus supports extensibility through pipes and filters, allowing cross-cutting concerns like logging, validation, and access control.

## Message Flow

```
PublishAsync(message)
        │
        ▼
┌───────────────────┐
│  Global Pipes     │ ◄── IMessagePipe (all messages)
│  (logging, etc.)  │     Return false to block
└───────┬───────────┘
        │
        ▼
┌───────────────────┐
│  Typed Pipes      │ ◄── IMessagePipe<TMessage> (specific type)
│  (validation)     │     Return false to block
└───────┬───────────┘
        │
        ▼
┌───────────────────┐
│  Get Consumers    │     Registered + subscribed consumers
└───────┬───────────┘
        │
        ▼
┌───────────────────┐
│  Consumer Filters │ ◄── IConsumerFilter (per-consumer)
│  (access control) │     Return false to skip consumer
└───────┬───────────┘
        │
        ▼
┌───────────────────┐
│  Consumer.Consume │     Parallel execution
└───────────────────┘
```

## Message Pipes

Pipes process messages before they reach consumers. They can:
- Log or monitor messages
- Validate message content
- Transform messages
- Block messages entirely

### IMessagePipe (Global)

Processes all messages regardless of type:

```csharp
public interface IMessagePipe
{
    Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class;
}
```

**Example: Logging Pipe**

```csharp
public class MessageLoggingPipe : IMessagePipe
{
    private readonly ILogger<MessageLoggingPipe> _logger;

    public MessageLoggingPipe(ILogger<MessageLoggingPipe> logger)
    {
        _logger = logger;
    }

    public Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        _logger.LogInformation(
            "MessageBus: Publishing {MessageType} - {Message}",
            typeof(TMessage).Name,
            message);

        return Task.FromResult(true); // Continue processing
    }
}
```

**Registration:**

```csharp
// Generic registration
services.AddMessagePipe<MessageLoggingPipe>();

// Or use the built-in convenience method
services.AddMessageLogging();
```

### IMessagePipe\<TMessage\> (Typed)

Processes only messages of a specific type:

```csharp
public interface IMessagePipe<TMessage> where TMessage : class
{
    Task<bool> ProcessAsync(TMessage message, CancellationToken cancellationToken);
}
```

**Example: Order Validation Pipe**

```csharp
public class OrderValidationPipe : IMessagePipe<CreateOrderMessage>
{
    public Task<bool> ProcessAsync(CreateOrderMessage message, CancellationToken cancellationToken)
    {
        if (message.Items.Count == 0)
        {
            // Block empty orders from being processed
            return Task.FromResult(false);
        }

        if (message.Total <= 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
```

**Registration:**

```csharp
services.AddMessagePipe<OrderValidationPipe, CreateOrderMessage>();
```

### Blocking Messages

Return `false` from `ProcessAsync` to prevent the message from reaching consumers:

```csharp
public class RateLimitingPipe : IMessagePipe
{
    private readonly Dictionary<Type, DateTime> _lastPublish = new();
    private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(100);

    public Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        var messageType = typeof(TMessage);

        if (_lastPublish.TryGetValue(messageType, out var last) &&
            DateTime.UtcNow - last < _minInterval)
        {
            return Task.FromResult(false); // Block rapid-fire messages
        }

        _lastPublish[messageType] = DateTime.UtcNow;
        return Task.FromResult(true);
    }
}
```

## Consumer Filters

Filters determine which consumers receive a message. Unlike pipes (which affect all consumers), filters are evaluated per-consumer.

### IConsumerFilter

```csharp
public interface IConsumerFilter
{
    bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message)
        where TMessage : class;
}
```

All registered filters must return `true` for a consumer to receive the message.

**Example: Plugin Filter**

The framework includes `DisabledPluginConsumerFilter` which prevents consumers from disabled plugins from receiving messages:

```csharp
public class DisabledPluginConsumerFilter : IConsumerFilter
{
    private readonly PluginState _pluginState;

    public DisabledPluginConsumerFilter(PluginState pluginState)
    {
        _pluginState = pluginState;
    }

    public bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message)
        where TMessage : class
    {
        var consumerAssembly = consumer.GetType().Assembly;

        // Find if consumer belongs to a plugin
        var plugin = _pluginState.Plugins
            .FirstOrDefault(p => p.Assembly == consumerAssembly);

        // Non-plugin consumers always receive messages
        if (plugin is null)
            return true;

        // Plugin consumers only receive if enabled
        return plugin.IsEnabled && _pluginState.PluginsActive;
    }
}
```

**Registration:**

```csharp
// Generic registration
services.AddConsumerFilter<DisabledPluginConsumerFilter>();

// Or use the convenience method
services.AddPluginConsumerFilter();
```

**Example: Role-Based Filter**

```csharp
public class RoleBasedConsumerFilter : IConsumerFilter
{
    private readonly ICurrentUser _currentUser;

    public RoleBasedConsumerFilter(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message)
        where TMessage : class
    {
        // Check if consumer requires specific role
        var requiredRole = consumer.GetType()
            .GetCustomAttribute<RequireRoleAttribute>()?.Role;

        if (requiredRole is null)
            return true;

        return _currentUser.HasRole(requiredRole);
    }
}

// Usage
[RequireRole("Admin")]
public class AdminNotificationConsumer : IConsumer<SystemAlertMessage>
{
    public Task Consume(SystemAlertMessage message)
    {
        // Only invoked for admin users
    }
}
```

## Built-in Extensions

### Message Logging

```csharp
services.AddMessageLogging();
```

Logs all messages at Information level. Useful for debugging message flow.

### Plugin Consumer Filter

```csharp
services.AddPluginConsumerFilter();
```

Prevents consumers from disabled plugins from receiving messages. Requires `AddPluginFramework()` to be called first.

## Registration Methods Summary

| Method | Purpose |
|--------|---------|
| `AddMessagePipe<TPipe>()` | Register global pipe |
| `AddMessagePipe<TPipe, TMessage>()` | Register typed pipe for specific message |
| `AddConsumerFilter<TFilter>()` | Register consumer filter |
| `AddMessageLogging()` | Add built-in logging pipe |
| `AddPluginConsumerFilter()` | Add built-in plugin filter |

## Order of Execution

1. **Global pipes** execute in registration order
2. **Typed pipes** execute after global pipes
3. **Consumer filters** evaluate for each consumer
4. **Consumers** execute in parallel (order not guaranteed)

## Best Practices

### Keep Pipes Fast

Pipes run synchronously in the message path. Avoid slow operations:

```csharp
// Good: Fast, synchronous check
public Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
{
    return Task.FromResult(IsValid(message));
}

// Avoid: Slow database call in pipe
public async Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
{
    var isAllowed = await _database.CheckPermissionAsync(message); // Slow!
    return isAllowed;
}
```

### Use Filters for Access Control

Filters are the right place for consumer-specific access control:

```csharp
// Good: Filter checks if consumer should receive message
public bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message)
{
    return HasPermission(consumer.GetType());
}

// Avoid: Pipe tries to handle access control globally
public Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
{
    // Can't know which consumers will receive the message here
}
```

### Combine Pipes and Filters

Use pipes for message-level concerns, filters for consumer-level:

```csharp
// Setup
services.AddMessagePipe<ValidationPipe>();           // Message validation
services.AddMessagePipe<AuditLoggingPipe>();        // Audit trail
services.AddConsumerFilter<PermissionFilter>();      // Access control
services.AddConsumerFilter<DisabledPluginConsumerFilter>(); // Plugin lifecycle
```
