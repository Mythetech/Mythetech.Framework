# Plugin Components

Plugin components are Blazor components that integrate into the host application's UI.

## Component Base Classes

### PluginComponentBase

All plugin components should inherit from `PluginComponentBase`:

```csharp
public abstract class PluginComponentBase : ComponentBase
```

**Injected Services:**
- `PluginContext` - Access to MessageBus, Services, StateStore

**Helper Methods:**

```csharp
// State management
T? GetState<T>(string key = "default");
void SetState<T>(string key, T value);

// Get plugin ID from manifest
string? GetPluginId();

// Asset loading
Task LoadStylesheetAsync(string href, string? integrity = null);
Task LoadScriptAsync(string src, string? integrity = null);

// Storage access (persistent)
IPluginStorage? Storage { get; }
```

### PluginMenu

For menu/toolbar integration:

```csharp
public abstract class PluginMenu : PluginComponentBase
{
    public abstract string Icon { get; }      // MudBlazor icon
    public abstract string Title { get; }     // Display title
    public virtual int Order => 100;          // Sort order (lower = first)
    public virtual string? Tooltip => null;   // Optional tooltip
    public virtual PluginMenuItem[]? Items => null;  // Menu items
}
```

**Example:**

```razor
@inherits PluginMenu

<MudStack>
    <MudText>Custom menu content</MudText>
</MudStack>

@code {
    public override string Icon => Icons.Material.Filled.Build;
    public override string Title => "My Tools";
    public override int Order => 50;

    public override PluginMenuItem[]? Items =>
    [
        new()
        {
            Icon = Icons.Material.Filled.Refresh,
            Text = "Refresh",
            OnClick = async ctx => await RefreshAsync()
        }
    ];
}
```

### PluginContextPanel

For side-panel/drawer integration:

```csharp
public abstract class PluginContextPanel : PluginComponentBase
{
    public abstract string Icon { get; }
    public abstract string Title { get; }
    public virtual int Order => 100;
    public virtual bool DefaultVisible => true;
}
```

**Example:**

```razor
@inherits PluginContextPanel

<MudPaper Class="pa-4">
    <MudText Typo="Typo.h6">Status Panel</MudText>
    <MudText>Connected: @_isConnected</MudText>
</MudPaper>

@code {
    public override string Icon => Icons.Material.Filled.Info;
    public override string Title => "Status";
    public override bool DefaultVisible => true;

    private bool _isConnected;
}
```

## Component Metadata

Use the `PluginComponentMetadataAttribute` for declarative metadata:

```csharp
[PluginComponentMetadata(
    Icon = "Icons.Material.Filled.Dashboard",
    Title = "My Panel",
    Order = 50,
    Tooltip = "Shows dashboard info")]
public class MyPanel : PluginContextPanel
{
    // Icon, Title, Order from attribute
    public override string Icon => Icons.Material.Filled.Dashboard;
    public override string Title => "My Panel";
}
```

**Precedence:**
1. `PluginComponentMetadataAttribute` (highest priority)
2. Property values from the component class
3. Default values

## Menu Items

`PluginMenuItem` defines clickable menu actions:

```csharp
public class PluginMenuItem
{
    public required string Icon { get; init; }
    public required string Text { get; init; }
    public required Func<PluginContext, Task> OnClick { get; init; }
    public bool Disabled { get; init; }
}
```

**Usage:**

```csharp
public override PluginMenuItem[]? Items =>
[
    new()
    {
        Icon = Icons.Material.Filled.Save,
        Text = "Save",
        OnClick = async context =>
        {
            var storage = context.StorageFactory?.CreateForPlugin(GetPluginId()!);
            await storage?.SetAsync("data", _myData);
        }
    },
    new()
    {
        Icon = Icons.Material.Filled.Delete,
        Text = "Clear",
        Disabled = !HasData,
        OnClick = async context => await ClearDataAsync()
    }
];
```

## PluginGuard

Wraps plugin components for error handling and lifecycle:

```razor
<PluginGuard PluginInfo="@plugin" Metadata="@metadata">
    <DynamicComponent Type="@metadata.ComponentType" />
</PluginGuard>
```

**Behavior:**
- Shows placeholder if plugin is deleted
- Shows "disabled" message if plugin is disabled
- Catches and displays rendering errors
- Optional stack trace display via `ShowErrorDetails`

## State Management

### Transient State (In-Memory)

Shared across components within a session:

```csharp
// Get state
var count = GetState<int>("counter");

// Set state (triggers StateChanged event)
SetState("counter", count + 1);

// Listen for changes
protected override void OnInitialized()
{
    Context.StateStore.StateChanged += OnStateChanged;
}

private void OnStateChanged(object? sender, PluginStateChangedEventArgs e)
{
    if (e.PluginId == GetPluginId())
    {
        StateHasChanged();
    }
}
```

### Persistent Storage

Survives application restarts:

```csharp
// Access storage
var storage = Storage;

// Save data
await storage?.SetAsync("settings", mySettings);

// Load data
var settings = await storage?.GetAsync<MySettings>("settings");

// Check existence
if (await storage?.ExistsAsync("settings") == true) { ... }

// Delete
await storage?.DeleteAsync("settings");

// List keys
var keys = await storage?.GetKeysAsync("prefix_");
```

## Message Bus Integration

### Publishing Messages

```csharp
// From PluginComponentBase
await PublishAsync(new MyMessage("data"));

// With configuration
await Context.PublishAsync(message, new PublishConfiguration
{
    FireAndForget = true
});
```

### Consuming Messages

Implement `IConsumer<TMessage>`:

```razor
@inherits PluginContextPanel
@implements IConsumer<DataUpdatedMessage>

@code {
    public Task Consume(DataUpdatedMessage message)
    {
        // Handle message
        _data = message.NewData;
        StateHasChanged();
        return Task.CompletedTask;
    }
}
```

**Note:** Disabled plugins don't receive messages (filtered by `DisabledPluginConsumerFilter`).

## Accessing Services

```csharp
// From context
var dialogService = Context.Services.GetRequiredService<IDialogService>();
var httpClient = Context.Services.GetService<HttpClient>();

// Link opening (if available)
Context.LinkOpenService?.OpenUrl("https://example.com");

// File saving (if available)
await Context.FileSaveService?.SaveFileAsync("data.json", jsonBytes);
```

## Rendering in Host

The host application renders plugin components:

```razor
@inject PluginState PluginState

@* Render all enabled menu components *@
@foreach (var metadata in PluginState.EnabledMenuComponentsMetadata)
{
    <PluginGuard PluginInfo="@GetPluginInfo(metadata)" Metadata="@metadata">
        <DynamicComponent Type="@metadata.ComponentType" />
    </PluginGuard>
}

@* Render enabled context panels *@
@foreach (var panelType in PluginState.EnabledContextPanelComponents)
{
    <DynamicComponent Type="@panelType" />
}
```

## Best Practices

1. **Use base classes** - Always extend `PluginComponentBase`, `PluginMenu`, or `PluginContextPanel`
2. **Prefer transient state** - Use `GetState`/`SetState` for UI state
3. **Use persistent storage sparingly** - Only for data that must survive restarts
4. **Handle null services** - Not all services are available on all platforms
5. **Clean up subscriptions** - Unsubscribe from events in `Dispose`
6. **Keep components focused** - One responsibility per component
