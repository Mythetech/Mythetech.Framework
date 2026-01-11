# Plugin Configuration

## Registration

### Basic Setup

```csharp
// Add plugin framework services
builder.Services.AddPluginFramework();

// Build and initialize
var app = builder.Build();
app.Services.UsePlugins("plugins");
```

### With Registry Configuration

```csharp
builder.Services.AddPluginFramework(options =>
{
    options.PluginRegistryUri = "https://example.com/plugins/registry.json";
});
```

## PluginRegistryOptions

```csharp
public class PluginRegistryOptions
{
    // Default: Azure CDN endpoint
    public const string DefaultRegistryUri =
        "https://cdn-endpnt-stmythetechglobal.azureedge.net/release/plugins/mythetech-plugin-registry.json";

    public string PluginRegistryUri { get; set; } = DefaultRegistryUri;
}
```

## Plugin Registry Format

Remote registry for discovering installable plugins:

```json
{
  "plugins": [
    {
      "id": "com.example.plugin",
      "name": "Example Plugin",
      "version": "1.2.0",
      "uri": "https://example.com/plugins/ExamplePlugin.zip",
      "supportedPlatforms": ["desktop", "webassembly"],
      "isDevPlugin": false,
      "isPreview": false
    }
  ]
}
```

### Registry Entry Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Plugin identifier |
| `name` | string | Display name |
| `version` | string | Semantic version |
| `uri` | string | Download URL (DLL or ZIP) |
| `supportedPlatforms` | string[] | "desktop", "webassembly", or both |
| `isDevPlugin` | bool | Only show in development mode |
| `isPreview` | bool | Mark as preview/beta |

## Registered Services

`AddPluginFramework()` registers:

| Service | Lifetime | Description |
|---------|----------|-------------|
| `PluginState` | Singleton | Central plugin manager |
| `PluginLoader` | Singleton | Plugin discovery and loading |
| `PluginStateStore` | Singleton | In-memory state storage |
| `IPluginAssetLoader` | Scoped | CSS/JS asset loading |
| `IPluginRegistryService` | Scoped | Registry fetching |
| `PluginContext` | Scoped | Injected into plugin components |

Additional services (platform-specific):
- `IPluginStorageFactory` - Persistent storage
- `ILinkOpenService` - URL opening
- `IFileSaveService` - File downloads

## Loading Methods

### From Directory

```csharp
// Load all plugins from directory (Desktop)
app.Services.UsePlugins("plugins");

// Absolute path
app.Services.UsePlugins("/opt/myapp/plugins");
```

**Expected Structure:**
```
plugins/
├── PluginA/
│   ├── PluginA.dll
│   └── dependencies...
└── PluginB/
    └── PluginB.dll
```

### From Assembly

```csharp
// Load from referenced assembly (WebAssembly)
app.Services.UsePlugin(typeof(MyPlugin.Manifest).Assembly);

// Multiple assemblies
app.Services.UsePlugin(assembly1);
app.Services.UsePlugin(assembly2);
```

## Platform Configuration

### Platform Enum

```csharp
public enum Platform
{
    Desktop,       // Photino/WebView
    WebAssembly    // Browser WASM
}
```

### Platform Detection

The framework detects the current platform. Plugins can declare supported platforms:

```csharp
public class MyManifest : IPluginManifest
{
    // Desktop only
    public Platform[]? SupportedPlatforms => [Platform.Desktop];

    // WebAssembly only
    public Platform[]? SupportedPlatforms => [Platform.WebAssembly];

    // Both (or null for both)
    public Platform[]? SupportedPlatforms => null;
}
```

## Asset Configuration

### Plugin Assets

```csharp
public class MyManifest : IPluginManifest
{
    public PluginAsset[] Assets =>
    [
        // Relative path (resolved to /_content/{assembly}/)
        PluginAsset.Css("css/styles.css"),
        PluginAsset.Js("js/scripts.js"),

        // With SRI
        PluginAsset.Css(
            "https://cdn.example.com/lib.css",
            integrity: "sha384-...",
            crossOrigin: "anonymous"
        ),

        // _content path (used as-is)
        PluginAsset.Css("_content/OtherLibrary/styles.css")
    ];
}
```

### Asset Loading Service

```csharp
// Manual asset loading in components
await LoadStylesheetAsync("css/additional.css");
await LoadScriptAsync("js/additional.js");

// Via IPluginAssetLoader
var loader = Context.AssetLoader;
await loader.LoadPluginAssetsAsync(pluginInfo);

// Check if loaded
if (!loader.IsLoaded("css/styles.css"))
{
    await loader.LoadStylesheetAsync("css/styles.css");
}
```

## Storage Configuration

Storage is platform-specific:

### Desktop Storage

- Implementation: LiteDB
- Location: Application data directory
- Configuration via `IPluginStorageFactory` registration

### WebAssembly Storage

- Implementation: localStorage
- Automatic serialization to JSON
- ~5MB limit per origin

## UI Components

### PluginManagementDialog

Full-featured plugin manager:

```razor
@inject IDialogService DialogService

<MudButton OnClick="OpenPluginManager">Manage Plugins</MudButton>

@code {
    private async Task OpenPluginManager()
    {
        await DialogService.ShowAsync<PluginManagementDialog>("Plugins");
    }
}
```

**Features:**
- List installed plugins
- Enable/disable toggle
- Delete plugins
- Browse registry
- Install from registry
- Import from URL

### StandardPluginMenu

Standard plugin menu integration:

```razor
<StandardPluginMenu />
```

**Provides:**
- Plugin submenu with all plugins
- Per-plugin actions (about, enable/disable, delete)
- Import from file/URL
- Open plugins directory (desktop)

### PluginGuard

Error boundary for plugin components:

```razor
<PluginGuard PluginInfo="@plugin" Metadata="@metadata" ShowErrorDetails="true">
    <DynamicComponent Type="@metadata.ComponentType" />
</PluginGuard>
```

## Message Bus Integration

Disabled plugins are automatically filtered from message bus:

```csharp
// Registered automatically by AddPluginFramework()
services.AddTransient<IConsumerFilter, DisabledPluginConsumerFilter>();
```

To manually check:

```csharp
var pluginState = services.GetRequiredService<PluginState>();

// Check if plugins are active globally
if (!pluginState.PluginsActive) return;

// Check specific plugin
var plugin = pluginState.GetPlugin("com.example.plugin");
if (plugin?.IsEnabled != true) return;
```

## Logging

Plugin operations are logged via `ILogger`:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();

    // Verbose plugin logging
    logging.AddFilter("Mythetech.Framework.Infrastructure.Plugins", LogLevel.Debug);
});
```

## Custom Services

Register platform-specific services before `AddPluginFramework()`:

```csharp
// Custom storage factory
builder.Services.AddSingleton<IPluginStorageFactory, MyStorageFactory>();

// Custom asset loader
builder.Services.AddScoped<IPluginAssetLoader, MyAssetLoader>();

// Custom registry service
builder.Services.AddScoped<IPluginRegistryService, MyRegistryService>();

// Then add framework
builder.Services.AddPluginFramework();
```
