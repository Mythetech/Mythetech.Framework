# Photino Best Practices

Lessons from building desktop Blazor apps with Photino.

## Entry Point

### Use Synchronous Main

**Always use `void Main`, never `async Task Main` with Photino.**

```csharp
// GOOD
[STAThread]
static void Main(string[] args)
{
    // ...
    app.Run();
}

// BAD - causes blank window on Windows
[STAThread]
static async Task Main(string[] args)
{
    // ...
    app.Run();
}
```

**Why:** The `[STAThread]` attribute is required for Photino on Windows (WebView2 needs STA threading). When combined with `async Task Main`, the runtime modifies the thread context in ways that can prevent WebView2 from initializing properly. Symptoms include:
- Black window with no content
- index.html never loads (splash screen doesn't appear)
- No errors or exceptions thrown

If you need async operations at startup, see if it can be refactored and deferred later on after the app starts.

If you REALLY need async operations at startup, use `.GetAwaiter().GetResult()`:

```csharp
if (args.Contains("--mcp"))
{
    RunMcpServerAsync(args).GetAwaiter().GetResult();
    return;
}
```

## Startup Performance

### Defer Async Work Until After Render

**Never perform blocking or async work in Program.cs before `app.Run()`.**

The goal is to minimize time from app launch to first WebView render. Users should see your splash screen immediately, not stare at a blank window.

```csharp
// BAD - blocks window from appearing
await LoadSettings();
await InitializeDatabase();
app.Run(); // Window only appears after above complete

// GOOD - window appears immediately, work happens after render
app.Run();
```

**Pattern:** Use `IAppAsyncInitializer` called from `MainLayout.OnAfterRenderAsync(firstRender)`:

```csharp
// In MainLayout.razor
@inject IAppAsyncInitializer AppInitializer

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await AppInitializer.InitializeAsync();
    }
}
```

```csharp
// AppAsyncInitializer.cs
public class AppAsyncInitializer : IAppAsyncInitializer
{
    private int _initialized;

    public async Task InitializeAsync()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
            return; // Already initialized

        await LoadSettingsAsync();
        await StartBackgroundServicesAsync();
        // etc.
    }
}
```

**Benefits:**
- Window renders immediately (splash screen visible)
- User sees responsiveness right away
- Centralized initialization with error handling
- Thread-safe single initialization via `Interlocked.Exchange`

## Splash Screens

### Use Pure CSS/HTML in index.html

Don't wait for Blazor to render a splash screen - put it directly in index.html with CSS animations:

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        #app-splash {
            position: fixed;
            inset: 0;
            background: linear-gradient(135deg, #0a0a0a 0%, #1a1a1a 100%);
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            z-index: 99999;
            opacity: 1;
            transition: opacity 0.4s ease-out;
        }
        #app-splash.fade-out { opacity: 0; pointer-events: none; }

        #app-splash .logo {
            animation: pulse 2s ease-in-out infinite;
        }
        @keyframes pulse {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.05); }
        }

        /* Add loading spinner, wave bars, etc. */
    </style>
</head>
<body>
    <div id="app-splash">
        <img class="logo" src="logo.png" alt="App" />
        <div class="title">App Name</div>
        <!-- CSS-animated loader here -->
    </div>

    <app id="app"></app>

    <script src="_framework/blazor.webview.js"></script>
    <script>
        window.appSplash = {
            hide: function() {
                var splash = document.getElementById('app-splash');
                if (splash) {
                    splash.classList.add('fade-out');
                    setTimeout(function() { splash.remove(); }, 400);
                }
            }
        };
    </script>
</body>
</html>
```

Then in MainLayout, after initialization completes:

```csharp
if (firstRender)
{
    await AppInitializer.InitializeAsync();
    await Task.Delay(300); // Optional: minimum splash time
    await JS.InvokeVoidAsync("appSplash.hide");
}
```

**Why pure HTML/CSS:**
- Appears instantly before Blazor loads
- No dependency on .NET runtime initialization
- Smooth animations even during heavy initialization
- Works even if Blazor fails to load (helps with debugging)

## Known Limitations

### Native Child Windows

Photino currently does not support true native child windows. You cannot:
- Create popup windows that are children of the main window
- Have modal dialogs that block the parent window natively
- Spawn secondary windows with parent-child relationships

**Workarounds:**
- Use in-app modal dialogs (MudBlazor dialogs, etc.)
- Use overlay panels instead of popups
- For multi-window needs, spawn independent windows (no parent relationship)

### Platform Differences

| Feature | Windows | macOS | Linux |
|---------|---------|-------|-------|
| WebView | WebView2 (Edge) | WKWebView | WebKitGTK |
| STA Threading | Required | N/A | N/A |
| Native menus | Limited | Limited | Limited |

## Project Configuration

### Recommended csproj Settings

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ApplicationIcon>wwwroot\logo.ico</ApplicationIcon>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)' == 'Release'">
        <PublishSingleFile>true</PublishSingleFile>
        <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    </PropertyGroup>

    <ItemGroup>
        <None Update="wwwroot\**">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </None>
    </ItemGroup>
</Project>
```

**Avoid:**
- `<SupportedPlatform Include="browser" />` - This is for Blazor WebAssembly, not desktop apps

### Window Configuration

```csharp
app.MainWindow
    .SetSize(1920, 1080)
    .SetUseOsDefaultSize(false)
    .SetFullScreen(false)
    .SetLogVerbosity(0)
    .SetSmoothScrollingEnabled(true)
    .SetJavascriptClipboardAccessEnabled(true)
    .SetTitle("App Name");
```

## Debugging

### Black Window on Windows

If you see a black window with no content:

1. **Check Main signature** - Must be `void Main`, not `async Task Main`
2. **Check wwwroot location** - Must be next to the executable
3. **Add diagnostic logging:**
   ```csharp
   var diagnosticPath = Path.Combine(
       Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
       "AppName", "startup-diagnostic.txt");
   File.WriteAllText(diagnosticPath, $"""
       AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}
       wwwroot exists: {Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"))}
       index.html exists: {File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html"))}
       """);
   ```

### WebView2 Not Found (Windows)

Ensure WebView2 runtime is installed. For development, it's usually present. For deployment, consider:
- Evergreen WebView2 (auto-updates, requires internet)
- Fixed version WebView2 (bundled, larger install size)

## File Paths

Getting file paths right is surprisingly hard in cross-platform desktop apps. There are multiple path resolution methods, each with different behavior across platforms and deployment scenarios.

### The Golden Rules

1. **Read-only app resources** → `AppContext.BaseDirectory`
2. **Writable user data** → `Environment.SpecialFolder.LocalApplicationData`
3. **Never use** → `Directory.GetCurrentDirectory()`
4. **Prefer** -> `Path.Combine()` handles cross platform path concatenation

### AppContext.BaseDirectory (Read-Only Resources)

Use for files bundled with your app that don't need to be modified:
- `wwwroot/` folder (CSS, JS, images)
- `appsettings.json`
- Bundled assets

```csharp
// Configuration loading
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)  // NOT Directory.GetCurrentDirectory()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

// Checking if wwwroot exists
var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
```

**Why not `Directory.GetCurrentDirectory()`?**
- Returns different paths depending on how the app was launched
- Launching from Start Menu, shortcut, or terminal all give different results
- On macOS, launching a `.app` bundle sets CWD to `/` or user's home
- Completely unreliable for finding app resources

### LocalApplicationData (Writable User Data)

Use for anything you need to write to disk:
- Databases (LiteDB, SQLite)
- User settings
- Plugins
- Cache files
- Logs

```csharp
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "YourAppName");

Directory.CreateDirectory(appDataPath); // Always ensure it exists

// Examples
var databasePath = Path.Combine(appDataPath, "data.db");
var settingsPath = Path.Combine(appDataPath, "settings.json");
var pluginsPath = Path.Combine(appDataPath, "plugins");
var logsPath = Path.Combine(appDataPath, "logs");
```

**Platform paths:**

| Platform | LocalApplicationData |
|----------|---------------------|
| Windows | `C:\Users\{user}\AppData\Local\YourAppName\` |
| macOS | `/Users/{user}/Library/Application Support/YourAppName/` |
| Linux | `/home/{user}/.local/share/YourAppName/` |

### Why AppContext.BaseDirectory is Read-Only

**macOS App Bundles:** The `.app` bundle is code-signed. Any modification invalidates the signature, and macOS will refuse to run the app. Never write to `AppContext.BaseDirectory` on macOS.

**Windows with Velopack:** After updates, the app directory changes (versioned folders). Data written to the old app directory won't be found after updates.

**Linux packages:** Similar issues with system-installed apps having restricted write permissions.

### Common Patterns

**Database storage:**
```csharp
public class AppRepository
{
    private static string GetDatabasePath()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YourAppName");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, "app.db");
    }
}
```

**Plugin storage:**
```csharp
// Plugins need writable location for downloads and state
var pluginDbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "YourAppName",
    "plugins.db");
Directory.CreateDirectory(Path.GetDirectoryName(pluginDbPath)!);
services.AddPluginStorage(pluginDbPath);
```

**Diagnostic logging (for debugging path issues):**
```csharp
var diagnosticPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "YourAppName", "startup-diagnostic.txt");

Directory.CreateDirectory(Path.GetDirectoryName(diagnosticPath)!);
File.WriteAllText(diagnosticPath, $"""
    Startup: {DateTime.Now}
    AppContext.BaseDirectory: {AppContext.BaseDirectory}
    Directory.GetCurrentDirectory(): {Directory.GetCurrentDirectory()}
    LocalApplicationData: {Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}
    wwwroot exists: {Directory.Exists(Path.Combine(AppContext.BaseDirectory, "wwwroot"))}
    """);
```

### Velopack Update Considerations

Velopack installs apps in versioned directories and handles updates by creating new version folders. This means:

1. **App directory changes after updates** - Don't store user data in `AppContext.BaseDirectory`
2. **Velopack manages its own update cache** - Don't interfere with update directories
3. **Always use LocalApplicationData for persistence** - It survives updates

```
# Windows Velopack structure
C:\Users\{user}\AppData\Local\YourAppName\
├── current\              # Symlink to active version (managed by Velopack)
├── app-1.0.0\           # Version 1.0.0
├── app-1.1.0\           # Version 1.1.0 (after update)
├── packages\            # Downloaded update packages
└── # Your data should NOT be here - Velopack owns this

# Your writable data goes here instead:
C:\Users\{user}\AppData\Local\YourAppName\
├── data.db              # Your database
├── settings.json        # Your settings
└── plugins\             # Your plugins
```

Wait, that's confusing - Velopack uses the same LocalApplicationData path. Let me clarify:

```
# Velopack app installation (managed by Velopack)
C:\Users\{user}\AppData\Local\{PackageId}\
├── current\
├── app-1.0.0\
└── packages\

# Your writable data (you manage this)
C:\Users\{user}\AppData\Local\YourAppName\
├── data.db
├── settings.json
└── plugins\
```

Use a consistent app name for your data directory that's separate from Velopack's package structure.

### Path Resolution Summary

| Need | Method | Example |
|------|--------|---------|
| App resources (wwwroot, config) | `AppContext.BaseDirectory` | `Path.Combine(AppContext.BaseDirectory, "wwwroot")` |
| User data (DB, settings) | `SpecialFolder.LocalApplicationData` | `Path.Combine(Environment.GetFolderPath(...), "AppName", "data.db")` |
| User's home directory | `SpecialFolder.UserProfile` | For default project paths, etc. |
| Temp files | `Path.GetTempPath()` | Short-lived files only |

**Never use:**
- `Directory.GetCurrentDirectory()` - Unreliable
- `Environment.CurrentDirectory` - Same problem
- Hardcoded paths - Not cross-platform
- Writing to `AppContext.BaseDirectory` - Breaks on macOS, survives updates poorly
