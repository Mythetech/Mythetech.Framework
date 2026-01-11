# MCP Configuration

## McpServerOptions

Configure the MCP server via `AddMcp()`:

```csharp
builder.Services.AddMcp(options =>
{
    // Server identification
    options.ServerName = "MyApp";
    options.ServerVersion = "1.0.0";

    // Protocol version (default: "2024-11-05")
    options.ProtocolVersion = "2024-11-05";

    // Tool execution timeout (default: 60 seconds)
    options.ToolTimeout = TimeSpan.FromSeconds(120);

    // HTTP transport settings
    options.HttpEnabled = true;
    options.HttpPort = 3333;
    options.HttpPath = "/mcp";
    options.HttpHost = "localhost";
});
```

## Options Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ServerName` | `string` | `"Mythetech.Framework"` | Name reported in initialize response |
| `ServerVersion` | `string?` | `null` | Version string (falls back to entry assembly version) |
| `ProtocolVersion` | `string` | `"2024-11-05"` | MCP protocol version |
| `ToolTimeout` | `TimeSpan` | `60 seconds` | Maximum time for tool execution |
| `HttpEnabled` | `bool` | `false` | Enable HTTP transport |
| `HttpPort` | `int` | `3333` | HTTP listen port |
| `HttpPath` | `string` | `"/mcp"` | HTTP endpoint path |
| `HttpHost` | `string` | `"localhost"` | HTTP bind host |

## stdio Mode Configuration

For CLI/stdio mode, use `TryRunMcpServerAsync`:

```csharp
static async Task Main(string[] args)
{
    if (await McpRegistrationExtensions.TryRunMcpServerAsync(args, options =>
    {
        options.ServerName = "MyApp";
        options.ServerVersion = "1.0.0";
    }))
    {
        return; // MCP mode handled
    }

    // Normal app startup...
}
```

### With Custom Services

```csharp
await McpRegistrationExtensions.TryRunMcpServerAsync(
    args,
    options => { options.ServerName = "MyApp"; },
    services =>
    {
        // Add custom logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        // Add custom services your tools need
        services.AddSingleton<IMyService, MyService>();
    },
    typeof(Program).Assembly // Tool assemblies
);
```

## HTTP Transport Configuration

### Port Fallback

If the configured port is unavailable, the HTTP transport tries:
1. Configured port
2. Configured port + 1
3. Configured port + 2
4. Configured port + 10
5. Port 0 (OS-assigned)

The actual port used is available via:

```csharp
var state = services.GetRequiredService<McpServerState>();
var endpoint = state.HttpEndpoint; // e.g., "http://localhost:3334/mcp"
```

### Security

The HTTP transport includes security measures:

- **Localhost binding** - Default host is `localhost` to prevent external access
- **Origin validation** - Rejects requests from non-localhost origins (DNS rebinding protection)
- **Session management** - Each client gets a unique session ID

## Logging

MCP components use `ILogger<T>`. Configure via standard .NET logging:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);

    // Verbose MCP logging
    logging.AddFilter("Mythetech.Framework.Infrastructure.Mcp", LogLevel.Debug);
});
```

## Telemetry

MCP operations emit OpenTelemetry activities:

- `RPC:{method}` - For each JSON-RPC request
- `Tool:{toolName}` - For each tool execution

Tags include:
- `mcp.method` - RPC method name
- `mcp.request_id` - Request ID
- `mcp.tool_name` - Tool being executed
- `mcp.success` - Whether operation succeeded
- `mcp.error_message` - Error details if failed

## Client Configuration

### Claude Desktop (stdio)

```json
// ~/Library/Application Support/Claude/claude_desktop_config.json (macOS)
// %APPDATA%\Claude\claude_desktop_config.json (Windows)
{
  "mcpServers": {
    "myapp": {
      "command": "/path/to/myapp",
      "args": ["--mcp"]
    }
  }
}
```

### Claude Desktop (HTTP)

```json
{
  "mcpServers": {
    "myapp": {
      "url": "http://localhost:3333/mcp"
    }
  }
}
```

**Note:** Claude Desktop currently prefers stdio transport. HTTP configuration syntax may vary by client.
