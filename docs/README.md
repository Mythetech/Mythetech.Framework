# Mythetech.Framework Documentation

Welcome to the Mythetech.Framework documentation. This framework provides infrastructure for building extensible Blazor applications targeting Desktop (Photino) and WebAssembly platforms.

## Infrastructure

### [MCP (Model Context Protocol)](./Infrastructure/Mcp/README.md)

Expose application functionality as tools for AI assistants like Claude.

- [Tools](./Infrastructure/Mcp/Tools.md) - Creating and registering MCP tools
- [Transports](./Infrastructure/Mcp/Transports.md) - stdio vs HTTP transport
- [Tool Management](./Infrastructure/Mcp/ToolManagement.md) - Enable/disable tools at runtime
- [Configuration](./Infrastructure/Mcp/Configuration.md) - Server options and settings

### [Plugins](./Infrastructure/Plugins/README.md)

Build and load dynamic plugin modules.

- [Creating Plugins](./Infrastructure/Plugins/CreatingPlugins.md) - How to build a plugin
- [Components](./Infrastructure/Plugins/Components.md) - UI component types and patterns
- [State and Storage](./Infrastructure/Plugins/StateAndStorage.md) - Managing plugin data
- [Lifecycle](./Infrastructure/Plugins/Lifecycle.md) - Plugin loading, enabling, and events
- [Configuration](./Infrastructure/Plugins/Configuration.md) - Options and registry setup

## Quick Links

### Getting Started

```csharp
// Add MCP
builder.Services.AddMcp(options => options.ServerName = "MyApp");
builder.Services.AddMcpTools();
app.Services.UseMcp();

// Add Plugins
builder.Services.AddPluginFramework();
app.Services.UsePlugins("plugins");
```

### Sample Applications

- `samples/SampleHost.Desktop` - Desktop application with MCP and Plugins
- `samples/SampleHost.WebAssembly` - WebAssembly application
- `samples/SamplePlugin` - Example plugin implementation

## Platform Support

| Feature | Desktop | WebAssembly |
|---------|---------|-------------|
| MCP (stdio) | Yes | No |
| MCP (HTTP) | Yes | No* |
| Plugin Loading (dynamic) | Yes | No |
| Plugin Loading (static) | Yes | Yes |
| Persistent Storage | LiteDB | localStorage |

*HTTP transport requires server-side hosting

## Contributing

See the main repository for contribution guidelines.
