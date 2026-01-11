# MCP (Model Context Protocol) Infrastructure

The MCP infrastructure provides a framework for exposing application functionality as tools that can be invoked by AI assistants like Claude.

## Overview

MCP is a protocol that allows AI assistants to interact with external tools and services. This framework implements:

- **Tool Registration** - Discover and register tools from assemblies
- **Tool Execution** - Route tool calls through a message bus with timeout handling
- **Transport Layer** - Support for stdio (CLI) and HTTP transports
- **Tool Management** - Enable/disable tools at runtime with optional persistence

## Quick Start

### 1. Add MCP Services

```csharp
// In your service configuration
builder.Services.AddMcp(options =>
{
    options.ServerName = "MyApp";
    options.ServerVersion = "1.0.0";
});

// Register tools from your assembly
builder.Services.AddMcpTools();
```

### 2. Initialize MCP

```csharp
// After building services
app.Services.UseMcp();
```

### 3. Create a Tool

```csharp
[McpTool(Name = "greet", Description = "Greets a user by name")]
public class GreetTool : IMcpTool<GreetInput>
{
    public Task<McpToolResult> ExecuteAsync(GreetInput input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(McpToolResult.Text($"Hello, {input.Name}!"));
    }
}

public class GreetInput
{
    [McpToolInput(Description = "The name to greet", Required = true)]
    public string Name { get; set; } = "";
}
```

## Architecture

```
┌─────────────────┐     ┌──────────────┐     ┌─────────────────┐
│   MCP Client    │────▶│  Transport   │────▶│   McpServer     │
│ (Claude, etc.)  │◀────│ (stdio/HTTP) │◀────│                 │
└─────────────────┘     └──────────────┘     └────────┬────────┘
                                                      │
                                                      ▼
                                             ┌─────────────────┐
                                             │   MessageBus    │
                                             └────────┬────────┘
                                                      │
                                                      ▼
                                             ┌─────────────────┐
                                             │ McpToolRegistry │
                                             │ + ToolHandler   │
                                             └────────┬────────┘
                                                      │
                                                      ▼
                                             ┌─────────────────┐
                                             │    IMcpTool     │
                                             │ Implementation  │
                                             └─────────────────┘
```

## Components

| Component | Description |
|-----------|-------------|
| `McpServer` | Handles JSON-RPC protocol messages |
| `McpServerState` | Manages server lifecycle and tool state |
| `McpToolRegistry` | Stores and manages tool descriptors |
| `McpToolLoader` | Discovers tools from assemblies via reflection |
| `McpToolCallHandler` | Executes tool calls through the message bus |
| `IMcpTransport` | Abstraction for message transport |

## Documentation

- [Tools](./Tools.md) - Creating and registering tools
- [Transports](./Transports.md) - stdio vs HTTP transport
- [Tool Management](./ToolManagement.md) - Enable/disable tools at runtime
- [Configuration](./Configuration.md) - Server options and settings
