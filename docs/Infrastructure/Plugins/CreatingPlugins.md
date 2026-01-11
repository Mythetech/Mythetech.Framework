# Creating Plugins

This guide walks through creating a plugin from scratch.

## Plugin Structure

A minimal plugin requires:

```
MyPlugin/
├── MyPlugin.csproj
├── MyPluginManifest.cs      # Required: IPluginManifest implementation
└── MyMenuComponent.razor    # Optional: UI components
```

## Step 1: Create the Project

```xml
<!-- MyPlugin.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MudBlazor" Version="..." />
    <ProjectReference Include="..\..\Mythetech.Framework\Mythetech.Framework.csproj" />
  </ItemGroup>
</Project>
```

## Step 2: Implement the Manifest

Every plugin must implement `IPluginManifest`:

```csharp
public class MyPluginManifest : IPluginManifest
{
    // Required properties
    public string Id => "com.example.myplugin";           // Unique identifier
    public string Name => "My Plugin";                     // Display name
    public Version Version => new(1, 0, 0);               // Semantic version
    public string Developer => "Example Corp";             // Developer/org name
    public string Description => "Does something useful";  // Brief description

    // Optional properties
    public string? Icon => Icons.Material.Filled.Extension;
    public string? ProjectUrl => "https://github.com/example/myplugin";
    public Version? MinimumFrameworkVersion => new(1, 0, 0);
    public bool IsPreview => false;
    public bool IsDevPlugin => false;

    // Platform targeting (null = all platforms)
    public Platform[]? SupportedPlatforms => null;
    // Or specific: [Platform.Desktop, Platform.WebAssembly]

    // CSS/JS assets to load
    public PluginAsset[] Assets => [
        PluginAsset.Css("css/styles.css"),
        PluginAsset.Js("js/plugin.js")
    ];
}
```

### Manifest Properties Reference

| Property | Required | Description |
|----------|----------|-------------|
| `Id` | Yes | Unique identifier (reverse domain notation recommended) |
| `Name` | Yes | Human-readable name |
| `Version` | Yes | Semantic version |
| `Developer` | Yes | Developer or organization name |
| `Description` | Yes | Brief description |
| `Icon` | No | MudBlazor icon path |
| `ProjectUrl` | No | Link to project/documentation |
| `MinimumFrameworkVersion` | No | Minimum compatible framework version |
| `SupportedPlatforms` | No | Array of supported platforms (null = all) |
| `Assets` | No | CSS/JS assets to load |
| `IsDevPlugin` | No | Only show in dev mode |
| `IsPreview` | No | Mark as preview/beta |

## Step 3: Add UI Components

### Menu Component

For toolbar/menu integration:

```razor
@using Mythetech.Framework.Infrastructure.Plugins.Components
@inherits PluginMenu

<MudText>Menu content here</MudText>

@code {
    public override string Icon => Icons.Material.Filled.Settings;
    public override string Title => "My Plugin";
    public override int Order => 50;  // Lower = appears first

    // Optional: Define menu items
    public override PluginMenuItem[]? Items =>
    [
        new()
        {
            Icon = Icons.Material.Filled.Add,
            Text = "Do Something",
            OnClick = async context =>
            {
                await DoSomethingAsync();
            }
        },
        new()
        {
            Icon = Icons.Material.Filled.Info,
            Text = "Show Info",
            OnClick = async context =>
            {
                // Access services via context
                var dialogService = context.Services.GetRequiredService<IDialogService>();
                await dialogService.ShowMessageBox("Info", "Hello from plugin!");
            }
        }
    ];

    private async Task DoSomethingAsync()
    {
        // Plugin logic here
        await PublishAsync(new MyPluginMessage("Action performed"));
    }
}
```

### Context Panel Component

For side-panel integration:

```razor
@using Mythetech.Framework.Infrastructure.Plugins.Components
@inherits PluginContextPanel
@implements IConsumer<MyPluginMessage>

[PluginComponentMetadata(
    Icon = "Icons.Material.Filled.Dashboard",
    Title = "My Panel",
    Order = 100)]

<MudPaper Class="pa-4">
    <MudText Typo="Typo.h6">@Title</MudText>
    <MudText>Last message: @_lastMessage</MudText>
</MudPaper>

@code {
    public override string Icon => Icons.Material.Filled.Dashboard;
    public override string Title => "My Panel";
    public override bool DefaultVisible => true;

    private string _lastMessage = "None";

    // Handle messages from other components
    public Task Consume(MyPluginMessage message)
    {
        _lastMessage = message.Text;
        StateHasChanged();
        return Task.CompletedTask;
    }
}
```

## Step 4: Add Assets (Optional)

### Define Assets in Manifest

```csharp
public PluginAsset[] Assets =>
[
    PluginAsset.Css("css/plugin-styles.css"),
    PluginAsset.Js("js/plugin-scripts.js"),

    // With integrity hash (SRI)
    PluginAsset.Css(
        "css/external.css",
        integrity: "sha384-...",
        crossOrigin: "anonymous"
    )
];
```

### Asset Path Resolution

| Path Format | Resolved To |
|-------------|-------------|
| `css/style.css` | `/_content/{AssemblyName}/css/style.css` |
| `_content/Other/style.css` | Used as-is |
| `https://cdn.example.com/style.css` | Used as-is |

### Project Configuration for Assets

```xml
<ItemGroup>
  <Content Include="css\**\*" />
  <Content Include="js\**\*" />
</ItemGroup>
```

## Step 5: Define Messages (Optional)

For communication between components:

```csharp
// MyPluginMessage.cs
public record MyPluginMessage(string Text);

// Usage in component
await PublishAsync(new MyPluginMessage("Something happened"));
```

## Step 6: Build and Deploy

### Desktop (Dynamic Loading)

1. Build the plugin: `dotnet build`
2. Copy output to plugins directory: `plugins/MyPlugin/`
3. Host loads automatically via `UsePlugins("plugins")`

### WebAssembly (Static Reference)

1. Add project reference in host
2. Load via `UsePlugin(typeof(MyPluginManifest).Assembly)`

## Complete Example

```csharp
// MyPluginManifest.cs
public class MyPluginManifest : IPluginManifest
{
    public string Id => "com.example.counter";
    public string Name => "Counter Plugin";
    public Version Version => new(1, 0, 0);
    public string Developer => "Example";
    public string Description => "A simple counter plugin";
    public string? Icon => Icons.Material.Filled.AddCircle;
}
```

```razor
@* CounterMenu.razor *@
@inherits PluginMenu

<MudText>Count: @Count</MudText>
<MudButton OnClick="Increment">+1</MudButton>

@code {
    public override string Icon => Icons.Material.Filled.AddCircle;
    public override string Title => "Counter";

    private int Count
    {
        get => GetState<int>("count");
        set => SetState("count", value);
    }

    private void Increment()
    {
        Count++;
    }
}
```

## Best Practices

1. **Use unique IDs** - Prefer reverse domain notation: `com.company.pluginname`
2. **Declare platform support** - Be explicit about Desktop vs WebAssembly compatibility
3. **Handle errors gracefully** - PluginGuard catches errors, but prevention is better
4. **Use state management** - Prefer `GetState`/`SetState` over component fields for shared state
5. **Minimize dependencies** - Keep plugins lightweight
6. **Version carefully** - Use semantic versioning for upgrades
