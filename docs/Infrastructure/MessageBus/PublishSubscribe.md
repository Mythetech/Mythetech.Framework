# Publish-Subscribe

The publish-subscribe pattern allows multiple consumers to react to the same message. Messages are fire-and-forget from the publisher's perspective.

## IConsumer Interface

```csharp
public interface IConsumer<TMessage>
{
    Task Consume(TMessage message);
}
```

Implement this interface to receive messages of type `TMessage`.

## Creating a Consumer

### Basic Consumer

```csharp
public class OrderCreatedMessage
{
    public string OrderId { get; init; }
    public decimal Total { get; init; }
}

public class NotifyWarehouseConsumer : IConsumer<OrderCreatedMessage>
{
    private readonly IWarehouseService _warehouse;

    public NotifyWarehouseConsumer(IWarehouseService warehouse)
    {
        _warehouse = warehouse;
    }

    public async Task Consume(OrderCreatedMessage message)
    {
        await _warehouse.PrepareOrder(message.OrderId);
    }
}
```

Consumers are resolved from the DI container, so constructor injection works normally.

### Multiple Consumers for Same Message

Multiple consumers can handle the same message type. All are invoked in parallel:

```csharp
// All of these receive OrderCreatedMessage
public class NotifyWarehouseConsumer : IConsumer<OrderCreatedMessage> { ... }
public class SendConfirmationEmailConsumer : IConsumer<OrderCreatedMessage> { ... }
public class UpdateAnalyticsConsumer : IConsumer<OrderCreatedMessage> { ... }
```

## Publishing Messages

### Basic Publishing

```csharp
[Inject]
private IMessageBus MessageBus { get; set; }

private async Task CreateOrder()
{
    // Create order logic...

    await MessageBus.PublishAsync(new OrderCreatedMessage
    {
        OrderId = order.Id,
        Total = order.Total
    });
}
```

### With Timeout and Cancellation

```csharp
await MessageBus.PublishAsync(
    new OrderCreatedMessage { OrderId = "123" },
    new PublishConfiguration
    {
        Timeout = TimeSpan.FromSeconds(10),
        CancellationToken = cancellationToken
    });
```

**PublishConfiguration Properties:**

| Property | Default | Description |
|----------|---------|-------------|
| `Timeout` | 30 seconds | Maximum time to wait for all consumers |
| `CancellationToken` | None | Token to cancel the operation |

## Registration

### Automatic Discovery

Consumers are automatically discovered and registered when using `AddMessageBus()`:

```csharp
// Discovers consumers in the framework assembly
builder.Services.AddMessageBus();

// Discovers consumers in multiple assemblies
builder.Services.AddMessageBus(
    typeof(Program).Assembly,
    typeof(MyPlugin).Assembly);
```

After the service provider is built:

```csharp
app.Services.UseMessageBus();
```

### Manual Registration

For explicit control:

```csharp
// Register consumer type (resolved from DI)
messageBus.RegisterConsumerType<OrderCreatedMessage, NotifyWarehouseConsumer>();
```

## ComponentConsumer for Blazor

Use `ComponentConsumer<TMessage>` when a Blazor component needs to receive messages:

```csharp
@inherits ComponentConsumer<ThemeChangedMessage>

<div class="@_themeClass">
    Content here
</div>

@code {
    private string _themeClass = "light";

    protected override async Task Consume(ThemeChangedMessage message, CancellationToken cancellationToken)
    {
        _themeClass = message.IsDark ? "dark" : "light";
        StateHasChanged();
    }
}
```

**Key features:**
- Auto-subscribes in `OnInitialized`
- Auto-unsubscribes in `Dispose`
- Wraps `Consume` in `InvokeAsync` for thread safety
- Provides `CancellationToken` for async operations

### Manual Component Subscription

For more control, subscribe manually:

```csharp
@implements IConsumer<ThemeChangedMessage>
@implements IDisposable

@code {
    [Inject] private IMessageBus MessageBus { get; set; }

    protected override void OnInitialized()
    {
        MessageBus.Subscribe(this);
    }

    public async Task Consume(ThemeChangedMessage message)
    {
        await InvokeAsync(() =>
        {
            // Update state
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        MessageBus.Unsubscribe(this);
    }
}
```

## Real-World Example

From the MCP infrastructure:

```csharp
// Message
public class EnableMcpServerMessage { }

// Consumer
public class EnableMcpServerConsumer : IConsumer<EnableMcpServerMessage>
{
    private readonly McpServerState _state;

    public EnableMcpServerConsumer(McpServerState state)
    {
        _state = state;
    }

    public async Task Consume(EnableMcpServerMessage message)
    {
        await _state.StartAsync();
    }
}

// Usage - from a menu component
private async Task EnableServer()
{
    await MessageBus.PublishAsync(new EnableMcpServerMessage());
}
```

## Exception Handling

Consumer exceptions are isolated - one consumer failing doesn't affect others:

```csharp
public class RiskyConsumer : IConsumer<SomeMessage>
{
    public async Task Consume(SomeMessage message)
    {
        throw new Exception("This won't stop other consumers");
    }
}

public class ReliableConsumer : IConsumer<SomeMessage>
{
    public async Task Consume(SomeMessage message)
    {
        // This still executes even if RiskyConsumer throws
        await DoWork();
    }
}
```

Exceptions are logged but not propagated to the publisher.

## Best Practices

### Message Design

```csharp
// Good: Immutable, descriptive
public class UserRegisteredMessage
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
}

// Avoid: Mutable, generic
public class Message
{
    public object Data { get; set; }
}
```

### Consumer Responsibility

Keep consumers focused on a single responsibility:

```csharp
// Good: Single responsibility
public class SendWelcomeEmailConsumer : IConsumer<UserRegisteredMessage> { ... }
public class CreateUserProfileConsumer : IConsumer<UserRegisteredMessage> { ... }

// Avoid: Multiple responsibilities
public class UserRegisteredConsumer : IConsumer<UserRegisteredMessage>
{
    public async Task Consume(UserRegisteredMessage message)
    {
        await SendEmail();
        await CreateProfile();
        await NotifyAdmins();
        await UpdateMetrics();
    }
}
```

### Idempotency

Design consumers to handle duplicate messages safely:

```csharp
public class ProcessPaymentConsumer : IConsumer<PaymentMessage>
{
    public async Task Consume(PaymentMessage message)
    {
        // Check if already processed
        if (await _repository.PaymentExists(message.PaymentId))
            return;

        await _paymentService.Process(message);
    }
}
```
