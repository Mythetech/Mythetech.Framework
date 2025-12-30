# Plugin Framework Samples

This directory contains example projects demonstrating how to use the Mythetech.Framework plugin system.

## ⚠️ Status: Initial Examples

**These sample host projects may be moved to a separate repository in the future.**

- `SamplePlugin/` - **Keep this** - A complete plugin example showing best practices
- `SampleHost.Desktop/` - Temporary example desktop host (Photino.Blazor)
- `SampleHost.WebAssembly/` - Temporary example WASM host

## Quick Start

### Creating a Plugin

See `SamplePlugin/` for a complete example. Key steps:

1. Create a Razor Class Library
2. Implement `IPluginManifest`
3. Create components inheriting from `PluginMenu` or `PluginContextPanel`
4. Reference `Mythetech.Framework` package

### Using Plugins in Your App

```csharp
// In Program.cs
builder.Services.AddPluginFramework();
// ...
host.Services.UsePlugins(); // Loads from plugins/ folder
```

## Reference Implementations

For production-ready examples, see:
- [Siren](https://github.com/Mythetech/Siren) - Desktop application using plugins
- [Apollo](https://github.com/Mythetech/Apollo) - Another desktop example
- [Aion](https://github.com/Mythetech/Aion) - Additional desktop example

## Future Plans

These sample hosts may be moved to a separate `Mythetech.Framework.Examples` repository once:
- Framework API stabilizes
- Real applications provide better reference implementations
- Maintenance burden becomes noticeable

The `SamplePlugin` example will remain in this repository as the canonical plugin example.

