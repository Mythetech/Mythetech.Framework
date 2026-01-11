# Plugin Lifecycle

This document covers how plugins are loaded, enabled/disabled, and removed.

## Loading Plugins

### From Directory (Desktop)

```csharp
// Load all plugins from a directory
app.Services.UsePlugins("plugins");
```

**Discovery Process:**
1. Scans directory and subdirectories for DLLs
2. Pre-loads all DLLs as dependencies
3. Identifies assemblies with `IPluginManifest` implementations
4. Creates `PluginLoadContext` for dependency isolation
5. Loads manifest and discovers component types
6. Registers with `PluginState`

**Directory Structure:**
```
plugins/
├── PluginA/
│   ├── PluginA.dll
│   └── PluginA.deps.json
└── PluginB/
    ├── PluginB.dll
    └── SomeDependency.dll
```

### From Assembly (WebAssembly)

```csharp
// Load from referenced assembly
app.Services.UsePlugin(typeof(MyPlugin.Manifest).Assembly);
```

WebAssembly doesn't support dynamic assembly loading, so plugins must be referenced at compile time.

### PluginLoader Details

The `PluginLoader` class handles discovery:

```csharp
public class PluginLoader
{
    // Load single plugin
    public PluginInfo? LoadPlugin(string dllPath);
    public PluginInfo? LoadPlugin(Assembly assembly, string? sourcePath = null);

    // Load all from directory
    public List<PluginInfo> LoadPluginsFromDirectory(string pluginDirectory);
}
```

## Dependency Isolation

Each plugin gets its own `PluginLoadContext`:

```csharp
public class PluginLoadContext : AssemblyLoadContext
{
    // Resolves dependencies from plugin directory first
    // Falls back to default context for shared assemblies
}
```

**Resolution Order:**
1. Plugin's directory
2. Default context (shared framework assemblies)

This prevents version conflicts when plugins use different versions of the same dependency.

## Plugin State

### PluginState Manager

Singleton managing all loaded plugins:

```csharp
public class PluginState
{
    // All loaded plugins
    public IReadOnlyList<PluginInfo> Plugins { get; }

    // Only enabled plugins (respects PluginsActive)
    public IReadOnlyList<PluginInfo> EnabledPlugins { get; }

    // Global enable/disable
    public bool PluginsActive { get; set; }

    // Enabled component types
    public IReadOnlyList<Type> EnabledMenuComponents { get; }
    public IReadOnlyList<Type> EnabledContextPanelComponents { get; }
}
```

### PluginInfo

Runtime representation of a loaded plugin:

```csharp
public class PluginInfo
{
    public IPluginManifest Manifest { get; }
    public Assembly Assembly { get; }
    public bool IsEnabled { get; set; }
    public DateTime LoadedAt { get; }
    public string? SourcePath { get; }

    // Discovered components
    public IReadOnlyList<Type> MenuComponents { get; }
    public IReadOnlyList<Type> ContextPanelComponents { get; }
    public IReadOnlyList<PluginComponentMetadata> MenuComponentsMetadata { get; }
    public IReadOnlyList<PluginComponentMetadata> ContextPanelComponentsMetadata { get; }

    // Version comparison
    public bool IsNewerThan(Version other);
    public bool IsSameVersion(Version other);
    public bool IsOlderThan(Version other);
}
```

## Enable/Disable

### Enabling a Plugin

```csharp
var pluginState = services.GetRequiredService<PluginState>();
pluginState.EnablePlugin("com.example.myplugin");
```

**Flow:**
1. `PluginEnabling` event fires (cancellable)
2. If not cancelled, `IsEnabled` set to `true`
3. `PluginEnabled` event fires
4. `StateChanged` event fires
5. Components now render and receive messages

### Disabling a Plugin

```csharp
pluginState.DisablePlugin("com.example.myplugin");
```

**Flow:**
1. `PluginDisabling` event fires (cancellable)
2. If not cancelled, `IsEnabled` set to `false`
3. `PluginDisabled` event fires
4. `StateChanged` event fires
5. Components stop rendering, messages filtered

### Global Toggle

```csharp
// Disable all plugins
pluginState.PluginsActive = false;

// Re-enable all plugins
pluginState.PluginsActive = true;
```

## Lifecycle Events

```csharp
public class PluginState
{
    // Before enabling (can cancel)
    public event EventHandler<PluginLifecycleEventArgs>? PluginEnabling;

    // After enabling
    public event EventHandler<PluginLifecycleEventArgs>? PluginEnabled;

    // Before disabling (can cancel)
    public event EventHandler<PluginLifecycleEventArgs>? PluginDisabling;

    // After disabling
    public event EventHandler<PluginLifecycleEventArgs>? PluginDisabled;

    // Any state change
    public event EventHandler? StateChanged;
}
```

**Event Args:**

```csharp
public class PluginLifecycleEventArgs : EventArgs
{
    public PluginInfo Plugin { get; }
    public bool Cancel { get; set; }  // Set to true to cancel
}
```

**Cancellation Example:**

```csharp
pluginState.PluginDisabling += (sender, args) =>
{
    if (args.Plugin.Manifest.Id == "critical.plugin")
    {
        args.Cancel = true;  // Prevent disabling
    }
};
```

## Removing Plugins

```csharp
pluginState.RemovePlugin("com.example.myplugin");
```

**Flow:**
1. Disables plugin if enabled
2. Removes from plugin list
3. `StateChanged` event fires
4. Optionally prompts for data deletion (if `IPersistentPlugin`)

### Persistent Data Cleanup

If a plugin implements `IPersistentPlugin`:

```csharp
public interface IPersistentPlugin
{
    string DataDisplayName { get; }
    string ExportFileExtension => "json";
    bool PromptDeleteOnRemove => true;
}
```

The UI can prompt to delete stored data when removing the plugin.

## Upgrades

### Automatic Upgrade Detection

```csharp
// Registers or upgrades if newer version
pluginState.RegisterOrUpgradePlugin(newPluginInfo);
```

**Behavior:**
- Same version: Rejected (duplicate)
- Newer version: Replaces existing
- Older version: Rejected

### Manual Upgrade Check

```csharp
var existing = pluginState.GetPlugin("com.example.plugin");
var newVersion = new Version(2, 0, 0);

if (existing != null && existing.IsOlderThan(newVersion))
{
    // Safe to upgrade
}
```

## Message Bus Filtering

`DisabledPluginConsumerFilter` prevents disabled plugins from receiving messages:

```csharp
public class DisabledPluginConsumerFilter : IConsumerFilter
{
    public bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message)
    {
        // Returns false if consumer's assembly belongs to disabled plugin
        // Also returns false if PluginsActive is false
    }
}
```

This is automatically registered when using `AddPluginFramework()`.

## Component Discovery

When loading a plugin, the framework discovers:

1. **Menu Components** - Classes inheriting from `PluginMenu`
2. **Context Panel Components** - Classes inheriting from `PluginContextPanel`

**Metadata Extraction:**
1. Check for `PluginComponentMetadataAttribute`
2. Fall back to property reflection
3. Apply defaults (icon, title, order)

## Error Handling

### Load Errors

```csharp
var plugins = loader.LoadPluginsFromDirectory("plugins");
// Failed plugins are logged but don't prevent others from loading
```

### Runtime Errors

`PluginGuard` catches rendering errors:

```razor
<PluginGuard PluginInfo="@plugin" ShowErrorDetails="true">
    <DynamicComponent Type="@componentType" />
</PluginGuard>
```

Displays error message and optionally stack trace without crashing the host.

## Lifecycle Diagram

```
                    ┌─────────────┐
                    │   DLL File  │
                    └──────┬──────┘
                           │
                    PluginLoader.LoadPlugin()
                           │
                           ▼
                    ┌─────────────┐
                    │  PluginInfo │
                    │ (Disabled)  │
                    └──────┬──────┘
                           │
              PluginState.RegisterPlugin()
                           │
                           ▼
                    ┌─────────────┐
                    │  Registered │◄────────────────┐
                    │  (Enabled)  │                 │
                    └──────┬──────┘                 │
                           │                        │
         ┌─────────────────┼─────────────────┐      │
         │                 │                 │      │
    DisablePlugin()   StateChanged()    EnablePlugin()
         │                 │                 │      │
         ▼                 ▼                 ▼      │
    ┌─────────────┐  ┌───────────┐    ┌───────────┐│
    │  Disabled   │  │    UI     │    │  Enabled  ││
    │(no messages)│  │  Updates  │    │(messages) │┘
    └──────┬──────┘  └───────────┘    └───────────┘
           │
     RemovePlugin()
           │
           ▼
    ┌─────────────┐
    │   Removed   │
    │(cleanup opt)│
    └─────────────┘
```
