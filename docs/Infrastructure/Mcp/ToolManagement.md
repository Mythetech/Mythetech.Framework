# Tool Management

The MCP framework supports runtime enabling/disabling of tools, allowing fine-grained control over which tools are exposed to AI clients.

## Overview

- Tools are **enabled by default** when registered
- Tools can be enabled/disabled at runtime via `McpServerState`
- Disabled tools are hidden from `tools/list` responses
- Calls to disabled tools return an error
- Persistence is **opt-in** via `IMcpToolStateProvider`

## Enabling/Disabling Tools

### Via McpServerState

```csharp
// Inject McpServerState
public class MyComponent
{
    private readonly McpServerState _mcpState;

    public MyComponent(McpServerState mcpState)
    {
        _mcpState = mcpState;
    }

    public async Task DisableDangerousTool()
    {
        await _mcpState.SetToolEnabledAsync("dangerous_tool", false);
    }

    public bool IsToolEnabled(string toolName)
    {
        return _mcpState.IsToolEnabled(toolName);
    }
}
```

### Via McpToolRegistry (Lower Level)

```csharp
var registry = services.GetRequiredService<McpToolRegistry>();

// Check if enabled
bool isEnabled = registry.IsToolEnabled("my_tool");

// Enable/disable
await registry.SetToolEnabledAsync("my_tool", false);

// Get only enabled tools
var enabledTools = registry.GetEnabledTools();

// Get all tools (regardless of state)
var allTools = registry.GetAllTools();
```

## UI Integration

The `McpInfoDialog` component includes toggle switches for each tool:

```razor
@inject McpServerState McpState

<MudSwitch T="bool"
           Value="@McpState.IsToolEnabled(tool.Name)"
           ValueChanged="@(async v => await McpState.SetToolEnabledAsync(tool.Name, v))"
           Color="Color.Primary" />
```

## Client Notifications

When tools are enabled/disabled via `McpServerState`:

1. The `StateChanged` event fires (for UI updates)
2. If the server is running with HTTP transport, a `notifications/tools/list_changed` notification is sent to connected clients
3. Clients can then call `tools/list` to get the updated tool list

**Important:** Notifications only work with HTTP transport. With stdio transport:
- Each session is a separate process
- Tool state changes require starting a new session
- Push notifications are not supported

## Persistence

By default, tool enabled/disabled state is **runtime-only** - it resets when the application restarts.

### Implementing Persistence

To persist tool state across restarts, implement `IMcpToolStateProvider`:

```csharp
public interface IMcpToolStateProvider
{
    Task<IReadOnlySet<string>> LoadDisabledToolsAsync();
    Task SaveDisabledToolsAsync(IReadOnlySet<string> disabledTools);
}
```

Example implementation:

```csharp
public class FileToolStateProvider : IMcpToolStateProvider
{
    private readonly string _filePath = "disabled_tools.json";

    public async Task<IReadOnlySet<string>> LoadDisabledToolsAsync()
    {
        if (!File.Exists(_filePath))
            return new HashSet<string>();

        var json = await File.ReadAllTextAsync(_filePath);
        var list = JsonSerializer.Deserialize<List<string>>(json) ?? new();
        return list.ToHashSet();
    }

    public async Task SaveDisabledToolsAsync(IReadOnlySet<string> disabledTools)
    {
        var json = JsonSerializer.Serialize(disabledTools.ToList());
        await File.WriteAllTextAsync(_filePath, json);
    }
}
```

Register before `AddMcp()`:

```csharp
builder.Services.AddSingleton<IMcpToolStateProvider, FileToolStateProvider>();
builder.Services.AddMcp();
```

The registry will automatically:
- Call `LoadStateAsync()` can be invoked to restore state on startup
- Call `SaveDisabledToolsAsync()` whenever a tool is enabled/disabled

## Behavior When Disabled

### tools/list Response

Disabled tools are **not included** in the `tools/list` response:

```json
{
  "tools": [
    { "name": "enabled_tool", "description": "..." }
    // disabled_tool is not listed
  ]
}
```

### tools/call for Disabled Tool

Calling a disabled tool returns an error:

```json
{
  "content": [
    {
      "type": "text",
      "text": "Tool 'disabled_tool' is disabled"
    }
  ],
  "isError": true
}
```

## Use Cases

### Conditional Tool Availability

```csharp
// Disable admin tools for non-admin users
if (!user.IsAdmin)
{
    await mcpState.SetToolEnabledAsync("delete_user", false);
    await mcpState.SetToolEnabledAsync("modify_permissions", false);
}
```

### Feature Flags

```csharp
// Disable experimental tools in production
if (environment.IsProduction())
{
    await mcpState.SetToolEnabledAsync("experimental_feature", false);
}
```

### User Preferences

```csharp
// Let users control which tools are available
foreach (var toolName in userPreferences.DisabledTools)
{
    await mcpState.SetToolEnabledAsync(toolName, false);
}
```
