using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Components;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginLoaderTests : IDisposable
{
    private readonly string _pluginDirectory = Path.Combine(Path.GetTempPath(), $"plugins_test_{Guid.NewGuid()}");
    private readonly PluginLoader _loader = new(NullLogger<PluginLoader>.Instance, new PluginState());

    public PluginLoaderTests()
    {
        Directory.CreateDirectory(_pluginDirectory);
    }

    [Fact(DisplayName = "LoadPluginsFromDirectory finds a plugin whose assembly is already loaded")]
    public void LoadPluginsFromDirectory_AlreadyLoadedAssembly_FindsPlugin()
    {
        CopyTestAssemblyTo(_pluginDirectory);

        var plugins = _loader.LoadPluginsFromDirectory(_pluginDirectory);

        var plugin = plugins.ShouldHaveSingleItem();
        plugin.Manifest.Id.ShouldBe(LoaderTestPluginManifest.PluginId);
        plugin.Assembly.ShouldBe(typeof(LoaderTestPluginManifest).Assembly);
    }

    [Fact(DisplayName = "LoadPluginsFromDirectory discovers a plugin's menu components and their metadata")]
    public void LoadPluginsFromDirectory_DiscoversMenuComponents()
    {
        CopyTestAssemblyTo(_pluginDirectory);

        var plugin = _loader.LoadPluginsFromDirectory(_pluginDirectory).ShouldHaveSingleItem();

        plugin.MenuComponents.ShouldContain(typeof(LoaderTestPluginMenu));
        plugin.MenuComponentsMetadata.ShouldContain(m => m.ComponentType == typeof(LoaderTestPluginMenu) && m.Title == "Loader Test Menu");
    }

    [Fact(DisplayName = "LoadPluginsFromDirectory scans each plugin's subdirectory")]
    public void LoadPluginsFromDirectory_PluginInSubdirectory_FindsPlugin()
    {
        var subdirectory = Path.Combine(_pluginDirectory, "LoaderTestPlugin");
        Directory.CreateDirectory(subdirectory);
        CopyTestAssemblyTo(subdirectory);

        var plugins = _loader.LoadPluginsFromDirectory(_pluginDirectory);

        plugins.ShouldHaveSingleItem().Manifest.Id.ShouldBe(LoaderTestPluginManifest.PluginId);
    }

    [Fact(DisplayName = "LoadPluginsFromDirectory skips a .dll file that is not an assembly")]
    public void LoadPluginsFromDirectory_FileThatIsNotAnAssembly_IsSkipped()
    {
        File.WriteAllText(Path.Combine(_pluginDirectory, "native.dll"), "not an assembly");
        CopyTestAssemblyTo(_pluginDirectory);

        var plugins = _loader.LoadPluginsFromDirectory(_pluginDirectory);

        plugins.ShouldHaveSingleItem().Manifest.Id.ShouldBe(LoaderTestPluginManifest.PluginId);
    }

    [Fact(DisplayName = "LoadPluginsFromDirectory returns no plugin for an assembly without a manifest")]
    public void LoadPluginsFromDirectory_AssemblyWithoutManifest_ReturnsNoPlugins()
    {
        var echoArgs = Path.Combine(AppContext.BaseDirectory, "Mythetech.Framework.Test.EchoArgs.dll");
        File.Copy(echoArgs, Path.Combine(_pluginDirectory, Path.GetFileName(echoArgs)));

        var plugins = _loader.LoadPluginsFromDirectory(_pluginDirectory);

        plugins.ShouldBeEmpty();
    }

    [Fact(DisplayName = "LoadPluginsFromDirectory returns no plugins when the directory does not exist")]
    public void LoadPluginsFromDirectory_MissingDirectory_ReturnsNoPlugins()
    {
        var plugins = _loader.LoadPluginsFromDirectory(Path.Combine(_pluginDirectory, "missing"));

        plugins.ShouldBeEmpty();
    }

    [Fact(DisplayName = "InitializePluginsAsync scans the plugin directory after the call returns")]
    public async Task InitializePluginsAsync_ScansDirectoryAfterTheCallReturns()
    {
        using var callReturned = new ManualResetEventSlim();
        bool? scannedBeforeCallReturned = null;
        var state = new PluginState();
        var logger = new CallbackLogger<PluginLoader>(() => scannedBeforeCallReturned ??= !callReturned.Wait(TimeSpan.FromSeconds(2)));
        state.SetPluginLoader(new PluginLoader(logger, state));
        CopyTestAssemblyTo(_pluginDirectory);

        var initialize = state.InitializePluginsAsync(_pluginDirectory);
        callReturned.Set();
        await initialize;

        scannedBeforeCallReturned.ShouldBe(false);
        state.Plugins.ShouldHaveSingleItem().Manifest.Id.ShouldBe(LoaderTestPluginManifest.PluginId);
    }

    private static void CopyTestAssemblyTo(string directory)
    {
        var assemblyPath = typeof(LoaderTestPluginManifest).Assembly.Location;
        File.Copy(assemblyPath, Path.Combine(directory, Path.GetFileName(assemblyPath)));
    }

    public void Dispose()
    {
        try { Directory.Delete(_pluginDirectory, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}

public class LoaderTestPluginManifest : IPluginManifest
{
    public const string PluginId = "mythetech.test.loader";

    public string Id => PluginId;
    public string Name => "Loader Test Plugin";
    public Version Version => new(1, 0, 0);
    public string Developer => "Mythetech";
    public string Description => "Plugin used by the plugin loader tests";
}

[PluginComponentMetadata(Title = "Loader Test Menu")]
public class LoaderTestPluginMenu : PluginMenu
{
    public override string Icon => string.Empty;
    public override string Title => "Loader Test Menu";
}

internal sealed class CallbackLogger<T>(Action onLog) : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => onLog();
}
