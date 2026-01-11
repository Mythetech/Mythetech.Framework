# State and Storage

Plugins have access to both transient (in-memory) and persistent storage.

## Transient State (PluginStateStore)

In-memory state shared across plugin components within a session.

### Basic Usage

From `PluginComponentBase`:

```csharp
// Get state (returns default if not set)
var count = GetState<int>("counter");

// Set state
SetState("counter", count + 1);

// With explicit plugin ID
var value = Context.StateStore.Get<string>(pluginId, "key");
Context.StateStore.SetForPlugin<string>(GetType(), "key", "value");
```

### State Change Events

```csharp
protected override void OnInitialized()
{
    Context.StateStore.StateChanged += OnStateChanged;
}

private void OnStateChanged(object? sender, PluginStateChangedEventArgs e)
{
    if (e.PluginId == GetPluginId() && e.Key == "mykey")
    {
        // React to state change
        StateHasChanged();
    }
}

public void Dispose()
{
    Context.StateStore.StateChanged -= OnStateChanged;
}
```

### Event Args

```csharp
public class PluginStateChangedEventArgs : EventArgs
{
    public string PluginId { get; }
    public string Key { get; }
}
```

### Clearing State

```csharp
// Clear all state for a plugin
Context.StateStore.ClearPlugin(pluginId);
```

### Use Cases

- UI state (selected tabs, expanded sections)
- Temporary data being edited
- Communication between components
- Cached computed values

## Persistent Storage (IPluginStorage)

Key-value storage that survives application restarts.

### Accessing Storage

From `PluginComponentBase`:

```csharp
// Storage property (may be null if not configured)
var storage = Storage;

if (storage != null)
{
    await storage.SetAsync("key", value);
}
```

Via context:

```csharp
var storage = Context.StorageFactory?.CreateForPlugin(GetPluginId()!);
```

### IPluginStorage Interface

```csharp
public interface IPluginStorage
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<IEnumerable<string>> GetKeysAsync(string? prefix = null);
    Task ClearAsync();
}
```

### Examples

**Save Settings:**

```csharp
public class MySettings
{
    public bool DarkMode { get; set; }
    public int RefreshInterval { get; set; } = 30;
}

// Save
await Storage?.SetAsync("settings", new MySettings
{
    DarkMode = true,
    RefreshInterval = 60
});

// Load
var settings = await Storage?.GetAsync<MySettings>("settings")
    ?? new MySettings();
```

**Store List of Items:**

```csharp
// Save
await Storage?.SetAsync("favorites", new List<string> { "item1", "item2" });

// Load
var favorites = await Storage?.GetAsync<List<string>>("favorites")
    ?? new List<string>();
```

**Check and Delete:**

```csharp
if (await Storage?.ExistsAsync("temp_data") == true)
{
    await Storage.DeleteAsync("temp_data");
}
```

**List Keys with Prefix:**

```csharp
// Get all keys starting with "cache_"
var cacheKeys = await Storage?.GetKeysAsync("cache_") ?? [];

foreach (var key in cacheKeys)
{
    await Storage.DeleteAsync(key);
}
```

**Clear All Plugin Data:**

```csharp
await Storage?.ClearAsync();
```

## Persistent Plugin Marker

Implement `IPersistentPlugin` for data management features:

```csharp
public class MyPluginManifest : IPluginManifest, IPersistentPlugin
{
    // IPluginManifest properties...

    // IPersistentPlugin
    public string DataDisplayName => "My Plugin Settings";
    public string ExportFileExtension => "json";
    public bool PromptDeleteOnRemove => true;
}
```

**Properties:**

| Property | Description |
|----------|-------------|
| `DataDisplayName` | Name shown in data management UI |
| `ExportFileExtension` | File extension for exports (default: "json") |
| `PromptDeleteOnRemove` | Ask to delete data when plugin removed |

## Data Import/Export

The `IPluginStorageFactory` supports data portability:

```csharp
var factory = Context.StorageFactory;

// Export all plugin data as JSON
string json = await factory.ExportPluginDataAsync(pluginId);

// Import data from JSON
await factory.ImportPluginDataAsync(pluginId, json);

// Delete all plugin data
await factory.DeletePluginDataAsync(pluginId);
```

## Platform-Specific Implementations

### Desktop (LiteDB)

- Uses LiteDB for file-based storage
- Data stored in application data directory
- Supports complex object graphs

### WebAssembly (localStorage)

- Uses browser localStorage
- Data serialized as JSON
- Size limits apply (~5MB typically)

## Best Practices

### When to Use Transient State

- Temporary UI state
- Data being actively edited
- Cross-component communication within session
- Cached computations

### When to Use Persistent Storage

- User preferences/settings
- Saved work that should survive restarts
- Authentication tokens
- Historical data

### Error Handling

```csharp
try
{
    var data = await Storage?.GetAsync<MyData>("key");
    if (data == null)
    {
        data = new MyData();  // Default if not found
    }
}
catch (Exception ex)
{
    // Handle deserialization errors gracefully
    _logger?.LogWarning(ex, "Failed to load plugin data");
    data = new MyData();
}
```

### Key Naming Conventions

```csharp
// Good: Descriptive, prefixed for organization
await Storage.SetAsync("settings.display", displaySettings);
await Storage.SetAsync("cache.users", userCache);
await Storage.SetAsync("history.searches", searchHistory);

// Avoid: Generic, collision-prone
await Storage.SetAsync("data", myData);  // Too generic
await Storage.SetAsync("temp", value);   // Consider transient state
```

### Serialization Considerations

Storage serializes to JSON. Ensure types are serializable:

```csharp
// Good: Simple POCO
public class Settings
{
    public string Theme { get; set; } = "default";
    public int FontSize { get; set; } = 14;
}

// Problematic: Circular references, complex types
public class BadSettings
{
    public Stream DataStream { get; set; }  // Can't serialize
    public BadSettings Parent { get; set; }  // Circular reference
}
```

### Migration Strategy

For evolving data formats:

```csharp
public class SettingsV2
{
    public int Version { get; set; } = 2;
    public string Theme { get; set; } = "default";
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}

// Load with migration
var settings = await Storage?.GetAsync<SettingsV2>("settings");
if (settings == null)
{
    // Try loading old format
    var oldSettings = await Storage?.GetAsync<SettingsV1>("settings");
    if (oldSettings != null)
    {
        settings = MigrateFromV1(oldSettings);
        await Storage.SetAsync("settings", settings);
    }
    else
    {
        settings = new SettingsV2();
    }
}
```
