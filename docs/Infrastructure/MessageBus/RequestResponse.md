# Request-Response

The request-response pattern allows sending a query and receiving a typed response. Unlike publish-subscribe, exactly one handler processes each query type.

## IQueryHandler Interface

```csharp
public interface IQueryHandler<TMessage, TResponse>
    where TMessage : class
    where TResponse : class
{
    Task<TResponse> Handle(TMessage message);
}
```

## Creating a Query Handler

### Basic Handler

```csharp
// Query message
public class GetProductQuery
{
    public required string ProductId { get; init; }
}

// Response
public class GetProductResponse
{
    public string Name { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
}

// Handler
public class GetProductHandler : IQueryHandler<GetProductQuery, GetProductResponse>
{
    private readonly IProductRepository _repository;

    public GetProductHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetProductResponse> Handle(GetProductQuery message)
    {
        var product = await _repository.GetByIdAsync(message.ProductId);

        return new GetProductResponse
        {
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        };
    }
}
```

## Sending Queries

### Basic Query

```csharp
[Inject]
private IMessageBus MessageBus { get; set; }

private async Task LoadProduct(string productId)
{
    var response = await MessageBus.SendAsync<GetProductQuery, GetProductResponse>(
        new GetProductQuery { ProductId = productId });

    _productName = response.Name;
    _productPrice = response.Price;
}
```

### With Timeout and Cancellation

```csharp
var response = await MessageBus.SendAsync<GetProductQuery, GetProductResponse>(
    new GetProductQuery { ProductId = productId },
    new QueryConfiguration
    {
        Timeout = TimeSpan.FromSeconds(5),
        CancellationToken = cancellationToken
    });
```

**QueryConfiguration Properties:**

| Property | Default | Description |
|----------|---------|-------------|
| `Timeout` | 30 seconds | Maximum time to wait for response |
| `CancellationToken` | None | Token to cancel the operation |

## Registration

### Automatic Discovery

Query handlers are automatically discovered with `AddMessageBus()`:

```csharp
builder.Services.AddMessageBus();
app.Services.UseMessageBus();
```

### Manual Registration

```csharp
messageBus.RegisterQueryHandler<GetProductQuery, GetProductResponse, GetProductHandler>();
```

**Note:** Only one handler can be registered per message type. Registering a second handler for the same message type overwrites the first.

## Error Handling

### No Handler Registered

If no handler is registered for a query, `SendAsync` throws `InvalidOperationException`:

```csharp
try
{
    var response = await MessageBus.SendAsync<UnknownQuery, SomeResponse>(query);
}
catch (InvalidOperationException ex)
{
    // "No handler registered for query type UnknownQuery"
}
```

### Handler Exceptions

Unlike consumers, handler exceptions propagate to the caller:

```csharp
public class FailingHandler : IQueryHandler<SomeQuery, SomeResponse>
{
    public Task<SomeResponse> Handle(SomeQuery message)
    {
        throw new InvalidOperationException("Something went wrong");
    }
}

// Caller
try
{
    var response = await MessageBus.SendAsync<SomeQuery, SomeResponse>(query);
}
catch (InvalidOperationException ex)
{
    // Handle the error
}
```

## Real-World Example

The MCP tool call handler uses request-response for routing tool executions:

```csharp
// Query
public class McpToolCallMessage
{
    public required string ToolName { get; init; }
    public JsonElement? Arguments { get; init; }
}

// Response
public class McpToolCallResponse
{
    public string ToolName { get; init; }
    public McpToolResult Result { get; init; }
}

// Handler
public class McpToolCallHandler : IQueryHandler<McpToolCallMessage, McpToolCallResponse>
{
    private readonly McpToolRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<McpToolCallHandler> _logger;

    public McpToolCallHandler(
        McpToolRegistry registry,
        IServiceProvider serviceProvider,
        ILogger<McpToolCallHandler> logger)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<McpToolCallResponse> Handle(McpToolCallMessage message)
    {
        var descriptor = _registry.GetTool(message.ToolName);
        if (descriptor is null)
        {
            return new McpToolCallResponse
            {
                ToolName = message.ToolName,
                Result = McpToolResult.Error($"Unknown tool: {message.ToolName}")
            };
        }

        var tool = (IMcpTool)_serviceProvider.GetRequiredService(descriptor.ToolType);
        var result = await tool.ExecuteAsync(input);

        return new McpToolCallResponse
        {
            ToolName = message.ToolName,
            Result = result
        };
    }
}

// Usage from MCP server
var response = await _messageBus.SendAsync<McpToolCallMessage, McpToolCallResponse>(
    new McpToolCallMessage
    {
        ToolName = toolName,
        Arguments = arguments
    });
```

## When to Use Request-Response vs Publish-Subscribe

| Scenario | Pattern |
|----------|---------|
| Need a result from the operation | Request-Response |
| Multiple handlers should react | Publish-Subscribe |
| Fire-and-forget notification | Publish-Subscribe |
| Querying data | Request-Response |
| Side effects without return value | Publish-Subscribe |
| Exactly one handler expected | Request-Response |

## Best Practices

### Query/Response Design

```csharp
// Good: Specific, typed
public class GetUserByEmailQuery
{
    public required string Email { get; init; }
}

public class GetUserByEmailResponse
{
    public string? UserId { get; init; }
    public string? Name { get; init; }
    public bool Found { get; init; }
}

// Avoid: Generic, untyped
public class GenericQuery
{
    public string Type { get; init; }
    public Dictionary<string, object> Parameters { get; init; }
}
```

### Null Handling

Design responses to handle "not found" scenarios:

```csharp
public class GetUserResponse
{
    public bool Found { get; init; }
    public UserDto? User { get; init; }
}

// Handler
public async Task<GetUserResponse> Handle(GetUserQuery message)
{
    var user = await _repository.FindByIdAsync(message.UserId);

    return new GetUserResponse
    {
        Found = user is not null,
        User = user is not null ? MapToDto(user) : null
    };
}
```

### Avoid Side Effects

Query handlers should be read-only where possible. Use consumers for operations with side effects:

```csharp
// Good: Query is read-only
public class GetOrderHandler : IQueryHandler<GetOrderQuery, GetOrderResponse>
{
    public async Task<GetOrderResponse> Handle(GetOrderQuery message)
    {
        return await _repository.GetOrderAsync(message.OrderId);
    }
}

// Side effects go through consumers
public class ProcessOrderConsumer : IConsumer<ProcessOrderMessage>
{
    public async Task Consume(ProcessOrderMessage message)
    {
        await _orderService.ProcessAsync(message.OrderId);
    }
}
```
