# MessageBus

The MessageBus provides in-memory publish-subscribe and request-response messaging for decoupled communication between components and services.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         IMessageBus                             │
├────────────────────────────┬────────────────────────────────────┤
│    Publish-Subscribe       │        Request-Response            │
│                            │                                    │
│  PublishAsync<T>()         │  SendAsync<TMsg, TResp>()          │
│  IConsumer<T>              │  IQueryHandler<TMsg, TResp>        │
│  Multiple consumers        │  Single handler per message type   │
│  Fire-and-forget           │  Returns typed response            │
└────────────────────────────┴────────────────────────────────────┘
```

## Quick Start

### Setup

```csharp
// In Program.cs or service configuration
builder.Services.AddMessageBus();

// After building the service provider
app.Services.UseMessageBus();
```

### Publishing Events

```csharp
public class UserCreatedMessage
{
    public string UserId { get; init; }
    public string Email { get; init; }
}

// Publish from any component or service
await _messageBus.PublishAsync(new UserCreatedMessage
{
    UserId = "123",
    Email = "user@example.com"
});
```

### Consuming Events

```csharp
public class SendWelcomeEmailConsumer : IConsumer<UserCreatedMessage>
{
    private readonly IEmailService _emailService;

    public SendWelcomeEmailConsumer(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Consume(UserCreatedMessage message)
    {
        await _emailService.SendWelcomeEmail(message.Email);
    }
}
```

Consumers are automatically discovered and registered when calling `AddMessageBus()`.

### Request-Response

```csharp
// Query message
public class GetUserQuery
{
    public string UserId { get; init; }
}

// Response
public class GetUserResponse
{
    public string Name { get; init; }
    public string Email { get; init; }
}

// Handler
public class GetUserHandler : IQueryHandler<GetUserQuery, GetUserResponse>
{
    public async Task<GetUserResponse> Handle(GetUserQuery message)
    {
        // Fetch user from database
        return new GetUserResponse { Name = "John", Email = "john@example.com" };
    }
}

// Usage
var response = await _messageBus.SendAsync<GetUserQuery, GetUserResponse>(
    new GetUserQuery { UserId = "123" });
```

## When to Use MessageBus

**Use MessageBus when:**
- Components need to communicate without direct dependencies
- Multiple consumers should react to the same event
- You want to decouple the sender from receivers
- Cross-cutting concerns (logging, validation) need to intercept messages

**Use direct service calls when:**
- You need a simple, synchronous operation
- There's only one handler and you need the result immediately
- Performance is critical (MessageBus has slight overhead)

## Topics

- [Publish-Subscribe](./PublishSubscribe.md) - Event distribution to multiple consumers
- [Request-Response](./RequestResponse.md) - Query pattern with typed responses
- [Extensibility](./Extensibility.md) - Pipes and filters for cross-cutting concerns
