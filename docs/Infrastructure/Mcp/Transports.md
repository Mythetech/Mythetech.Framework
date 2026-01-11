# MCP Transports

The MCP framework supports multiple transport mechanisms for communication between clients and the server.

## Transport Types

### stdio Transport (Default)

The stdio transport uses standard input/output streams for communication. This is the default transport and is ideal for CLI integrations.

**Characteristics:**
- Process-per-session model
- Client spawns the server process
- Communication via stdin/stdout
- No persistent connection between sessions

**Use Cases:**
- Claude Desktop integration
- CLI tool integration
- Simple deployments

**Configuration:**

```json
// claude_desktop_config.json
{
  "mcpServers": {
    "myapp": {
      "command": "/path/to/myapp",
      "args": ["--mcp"]
    }
  }
}
```

**Server Setup:**

```csharp
// At the start of Main()
if (await McpRegistrationExtensions.TryRunMcpServerAsync(args, options =>
{
    options.ServerName = "MyApp";
    options.ServerVersion = "1.0.0";
}))
{
    return; // MCP mode handled, exit
}

// Normal app startup continues...
```

### HTTP Transport

The HTTP transport enables MCP over HTTP POST requests, implementing the "Streamable HTTP" transport specification.

**Characteristics:**
- Persistent server instance
- Multiple clients can connect
- Supports real-time notifications
- Session management with `Mcp-Session-Id` header

**Use Cases:**
- Web applications
- Multi-client scenarios
- When you need push notifications (e.g., `tools/list_changed`)

**Configuration:**

```json
// claude_desktop_config.json (if client supports HTTP)
{
  "mcpServers": {
    "myapp": {
      "url": "http://localhost:3333/mcp"
    }
  }
}
```

**Server Setup:**

```csharp
// Register HTTP transport BEFORE AddMcp()
builder.Services.AddMcpHttpTransport();
builder.Services.AddMcp(options =>
{
    options.HttpEnabled = true;
    options.HttpPort = 3333;
    options.HttpPath = "/mcp";
});

// Start the server
await app.Services.StartMcpHttpServerAsync();
```

## Transport Comparison

| Feature | stdio | HTTP |
|---------|-------|------|
| Connection Model | Process per session | Persistent server |
| Client Spawns Server | Yes | No |
| Push Notifications | No* | Yes |
| Multiple Clients | No | Yes |
| Session State | Per-process | Server-managed |
| Platform Support | All | Non-browser |

*stdio notifications are written to stdout but clients typically don't poll for them

## HTTP Transport Options

```csharp
builder.Services.AddMcp(options =>
{
    // Enable HTTP transport
    options.HttpEnabled = true;

    // Port to listen on (default: 3333)
    // Falls back to +1, +2, +10, then auto-select if unavailable
    options.HttpPort = 3333;

    // URL path for MCP endpoint (default: "/mcp")
    options.HttpPath = "/mcp";

    // Host to bind to (default: "localhost" for security)
    options.HttpHost = "localhost";
});
```

## Security Considerations

### HTTP Transport

- **Localhost only by default** - The HTTP transport binds to `localhost` to prevent external access
- **Origin validation** - Requests are validated to prevent DNS rebinding attacks
- **Session tokens** - Each connection gets a unique session ID

### stdio Transport

- **Process isolation** - Each session runs in its own process
- **No network exposure** - Communication is purely via stdin/stdout

## Notifications

The HTTP transport supports server-to-client notifications:

```csharp
// Send tools list changed notification
await server.NotifyToolsListChangedAsync();
```

**Important:** Notifications only work with HTTP transport. With stdio, clients won't receive push notifications - changes require a new session.

## Implementing Custom Transports

Implement `IMcpTransport`:

```csharp
public interface IMcpTransport : IAsyncDisposable
{
    Task<JsonRpcRequest?> ReadMessageAsync(CancellationToken cancellationToken = default);
    Task WriteMessageAsync(JsonRpcResponse response, CancellationToken cancellationToken = default);
    Task WriteNotificationAsync(string method, object? @params, CancellationToken cancellationToken = default);
}
```

Register before `AddMcp()`:

```csharp
services.AddSingleton<IMcpTransport, MyCustomTransport>();
services.AddMcp();
```
