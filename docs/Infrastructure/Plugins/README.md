# Plugin Infrastructure

The Plugin infrastructure provides a complete system for extending applications with dynamically loaded modules, including discovery, loading, lifecycle management, UI integration, and inter-plugin communication.

## Overview

- **Dynamic Loading** - Load plugins from DLLs at runtime with dependency isolation
- **Component Discovery** - Automatic discovery of menu and panel components
- **Lifecycle Management** - Enable/disable/remove plugins at runtime
- **State Management** - Both transient (in-memory) and persistent storage
- **Message Bus Integration** - Inter-plugin and plugin-host communication
- **Asset Loading** - Dynamic CSS/JS injection
- **Platform Support** - Desktop (Photino) and WebAssembly targets

## Quick Start

### 1. Add Plugin Framework Services

```csharp
builder.Services.AddPluginFramework();
```

### 2. Load Plugins

```csharp
// From a directory (Desktop)
app.Services.UsePlugins("plugins");

// From a referenced assembly (WebAssembly)
app.Services.UsePlugin(typeof(MyPlugin.PluginManifest).Assembly);
```

### 3. Create a Plugin

```csharp
// PluginManifest.cs
public class MyPluginManifest : IPluginManifest
{
    public string Id => "com.example.myplugin";
    public string Name => "My Plugin";
    public Version Version => new(1, 0, 0);
    public string Developer => "Example Corp";
    public string Description => "Does something useful";
}
```

## Architecture

```
┌─────────────────────┐
│   Host Application  │
├─────────────────────┤
│    PluginState      │◀──── Manages all loaded plugins
│    PluginLoader     │◀──── Discovers & loads from assemblies
│    PluginContext    │◀──── Services injected into plugins
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐     ┌─────────────────────┐
│  Plugin Assembly A  │     │  Plugin Assembly B  │
├─────────────────────┤     ├─────────────────────┤
│  IPluginManifest    │     │  IPluginManifest    │
│  PluginMenu(s)      │     │  PluginContextPanel │
│  PluginContextPanel │     │  IConsumer<T>       │
└─────────────────────┘     └─────────────────────┘
```

## Core Components

| Component | Description |
|-----------|-------------|
| `IPluginManifest` | Required interface defining plugin metadata |
| `PluginState` | Singleton managing all loaded plugins |
| `PluginLoader` | Discovers and loads plugins from assemblies |
| `PluginContext` | Services and utilities injected into plugin components |
| `PluginComponentBase` | Base class for plugin Blazor components |
| `PluginMenu` | Base class for menu-based plugin UI |
| `PluginContextPanel` | Base class for side-panel plugin UI |
| `PluginGuard` | Error boundary wrapping plugin components |

## Documentation

- [Creating Plugins](./CreatingPlugins.md) - How to build a plugin
- [Plugin Components](./Components.md) - UI component types and patterns
- [State and Storage](./StateAndStorage.md) - Managing plugin data
- [Lifecycle](./Lifecycle.md) - Plugin loading, enabling, and events
- [Configuration](./Configuration.md) - Options and registry setup
