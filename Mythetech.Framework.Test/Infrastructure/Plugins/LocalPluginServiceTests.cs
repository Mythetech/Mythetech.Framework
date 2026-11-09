using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Mythetech.Framework.Desktop.Plugins;
using Mythetech.Framework.Infrastructure.Commands;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Consumers;
using Mythetech.Framework.Infrastructure.Plugins.Events;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
using Mythetech.Framework.Infrastructure.Plugins.Local;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Settings.Events;
using Mythetech.Framework.Test.Infrastructure.Plugins.TestPlugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class LocalPluginServiceTests : IAsyncDisposable
{
    private static readonly TimeSpan ReloadDeadline = TimeSpan.FromSeconds(5);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"local_plugin_test_{Guid.NewGuid():N}");
    private readonly string _pluginsDirectory;
    private readonly string _copiesRoot;
    private readonly string _binFolder;
    private readonly PluginState _state = new();
    private readonly PluginLoader _loader;
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly ConcurrentQueue<AddNotification> _notifications = new();
    private readonly RecordingLogger<LocalPluginService> _logger = new();
    private readonly List<LocalPluginService> _services = [];

    public LocalPluginServiceTests()
    {
        _pluginsDirectory = Path.Combine(_root, "plugins");
        _copiesRoot = Path.Combine(_root, "copies");
        _binFolder = Path.Combine(_root, "src", "bin");
        Directory.CreateDirectory(_pluginsDirectory);
        _state.SetPluginDirectory(_pluginsDirectory);
        _loader = new PluginLoader(NullLogger<PluginLoader>.Instance, _state);
        _bus.When(bus => bus.PublishAsync(Arg.Any<AddNotification>()))
            .Do(call => _notifications.Enqueue(call.Arg<AddNotification>()));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "AddAsync registers a copy of the build as a local plugin and saves its path")]
    public async Task AddAsync_RegistersLocalPluginAndSavesPath()
    {
        const string id = "mythetech.test.local-added";
        var name = UniqueName("Added");
        var dllPath = TestPluginBuilder.Build(_binFolder, name, id, new Version(1, 0, 0));
        var service = CreateService();

        var result = await service.AddAsync(dllPath);

        result.Succeeded.ShouldBeTrue();
        result.Message.ShouldBe($"Added local plugin {name}.");
        var plugin = _state.GetPlugin(id).ShouldNotBeNull();
        plugin.IsLocal.ShouldBeTrue();
        result.Plugins.ShouldBe([plugin]);
        PluginInstallLayout.IsInside(plugin.SourcePath.ShouldNotBeNull(), _copiesRoot).ShouldBeTrue();
        service.Entries.ShouldBe([new LocalPluginEntry(id, dllPath)]);
        ReadSavedPaths().ShouldBe([dllPath]);
    }

    [Fact(DisplayName = "AddAsync of a file without a plugin fails and saves nothing")]
    public async Task AddAsync_NoPlugin_Fails()
    {
        Directory.CreateDirectory(_binFolder);
        var path = Path.Combine(_binFolder, "NotAPlugin.dll");
        await File.WriteAllTextAsync(path, "not an assembly", Ct);
        var service = CreateService();

        var result = await service.AddAsync(path);

        result.Succeeded.ShouldBeFalse();
        result.Plugins.ShouldBeEmpty();
        result.Message.ShouldBe("No plugin was found in that file.");
        service.Entries.ShouldBeEmpty();
        File.Exists(SavedPathsFile).ShouldBeFalse();
    }

    [Fact(DisplayName = "AddAsync with the ID of an installed plugin registers the local build in its place, whatever its version")]
    public async Task AddAsync_SameIdAsInstalled_ShadowsInstalled()
    {
        const string id = "mythetech.test.local-shadow";
        await InstallAsync(id, new Version(1, 0, 0));
        var dllPath = TestPluginBuilder.Build(_binFolder, UniqueName("Shadow"), id, new Version(0, 9, 0));
        var service = CreateService();

        var result = await service.AddAsync(dllPath);

        result.Succeeded.ShouldBeTrue();
        var plugin = _state.GetPlugin(id).ShouldNotBeNull();
        plugin.IsLocal.ShouldBeTrue();
        plugin.Manifest.Version.ShouldBe(new Version(0, 9, 0));
        _state.Plugins.Count(p => p.Manifest.Id == id).ShouldBe(1);
    }

    [Fact(DisplayName = "RemoveAsync of a local plugin that replaced an install loads the install back")]
    public async Task RemoveAsync_RestoresShadowedInstall()
    {
        const string id = "mythetech.test.local-restore";
        var installed = await InstallAsync(id, new Version(1, 0, 0));
        var dllPath = TestPluginBuilder.Build(_binFolder, UniqueName("Restore"), id, new Version(2, 0, 0));
        var service = CreateService();
        await service.AddAsync(dllPath);

        var removed = await service.RemoveAsync(id);

        removed.ShouldBeTrue();
        var restored = _state.GetPlugin(id).ShouldNotBeNull();
        restored.IsLocal.ShouldBeFalse();
        restored.Manifest.Version.ShouldBe(new Version(1, 0, 0));
        Path.GetDirectoryName(restored.SourcePath).ShouldBe(Path.GetDirectoryName(installed.SourcePath));
        service.Entries.ShouldBeEmpty();
        ReadSavedPaths().ShouldBeEmpty();
    }

    [Fact(DisplayName = "RemoveAsync of a local plugin without an install unregisters it and keeps its build output and data")]
    public async Task RemoveAsync_WithoutShadow_UnregistersAndKeepsFilesAndData()
    {
        const string id = "mythetech.test.local-remove";
        var storage = Substitute.For<IPluginStorageFactory>();
        _state.SetStorageFactory(storage);
        var dllPath = TestPluginBuilder.Build(_binFolder, UniqueName("Remove"), id, new Version(1, 0, 0));
        var service = CreateService();
        await service.AddAsync(dllPath);

        var removed = await service.RemoveAsync(id);

        removed.ShouldBeTrue();
        _state.GetPlugin(id).ShouldBeNull();
        File.Exists(dllPath).ShouldBeTrue();
        await storage.DidNotReceive().DeletePluginDataAsync(Arg.Any<string>());
        service.Entries.ShouldBeEmpty();
        ReadSavedPaths().ShouldBeEmpty();
    }

    [Fact(DisplayName = "RemoveAsync of an ID that isn't a local plugin returns false")]
    public async Task RemoveAsync_UnknownId_ReturnsFalse()
    {
        const string id = "mythetech.test.local-not-tracked";
        await InstallAsync(id, new Version(1, 0, 0));
        var service = CreateService();

        var removed = await service.RemoveAsync(id);

        removed.ShouldBeFalse();
        _state.GetPlugin(id).ShouldNotBeNull();
    }

    [Fact(DisplayName = "Rebuilding a local plugin reloads it")]
    public async Task Rebuild_ReloadsPlugin()
    {
        const string id = "mythetech.test.local-rebuild";
        var name = UniqueName("Rebuild");
        var dllPath = TestPluginBuilder.Build(_binFolder, name, id, new Version(1, 0, 0));
        var service = CreateService();
        await service.AddAsync(dllPath);

        TestPluginBuilder.Build(_binFolder, name, id, new Version(1, 0, 1));

        await WaitUntilAsync(() => _state.GetPlugin(id)?.Manifest.Version == new Version(1, 0, 1));
        var plugin = _state.GetPlugin(id).ShouldNotBeNull();
        plugin.Manifest.Version.ShouldBe(new Version(1, 0, 1));
        plugin.IsLocal.ShouldBeTrue();
        service.Entries.ShouldBe([new LocalPluginEntry(id, dllPath)]);
    }

    [Fact(DisplayName = "Two rebuilds within 100 ms end with the latest build registered once")]
    public async Task TwoRapidRebuilds_EndWithLatestRegisteredOnce()
    {
        const string id = "mythetech.test.local-rapid";
        var name = UniqueName("Rapid");
        var dllPath = TestPluginBuilder.Build(_binFolder, name, id, new Version(1, 0, 0));
        var second = TestPluginBuilder.Build(StagingFolder(), name, id, new Version(2, 0, 0));
        var third = TestPluginBuilder.Build(StagingFolder(), name, id, new Version(3, 0, 0));
        var service = CreateService();
        await service.AddAsync(dllPath);

        File.Move(second, dllPath, overwrite: true);
        File.Move(third, dllPath, overwrite: true);

        await WaitUntilAsync(() => _state.GetPlugin(id)?.Manifest.Version == new Version(3, 0, 0));
        _state.GetPlugin(id).ShouldNotBeNull().Manifest.Version.ShouldBe(new Version(3, 0, 0));
        _state.Plugins.Count(p => p.Manifest.Id == id).ShouldBe(1);
        _notifications.ShouldBeEmpty();
        _logger.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "A rebuild that lands while a reload is running ends with the latest build registered once")]
    public async Task RebuildDuringReload_EndsWithLatestRegisteredOnce()
    {
        const string id = "mythetech.test.local-during";
        var name = UniqueName("During");
        var dllPath = TestPluginBuilder.Build(_binFolder, name, id, new Version(1, 0, 0));
        var third = TestPluginBuilder.Build(StagingFolder(), name, id, new Version(3, 0, 0));
        // Loading 2.0.0 moves 3.0.0 into the build folder, so the watcher fires in the middle of that reload.
        var movesThirdOnLoad = $$"""
            namespace {{TestPluginBuilder.ManifestNamespace}};

            public partial class Manifest
            {
                public Manifest()
                {
                    var next = {{Literal(third)}};
                    if (System.IO.File.Exists(next))
                        System.IO.File.Move(next, {{Literal(dllPath)}}, true);
                }
            }
            """;
        var second = TestPluginBuilder.Build(StagingFolder(), name, id, new Version(2, 0, 0), movesThirdOnLoad);
        var service = CreateService();
        await service.AddAsync(dllPath);

        File.Move(second, dllPath, overwrite: true);

        await WaitUntilAsync(() => _state.GetPlugin(id)?.Manifest.Version == new Version(3, 0, 0));
        _state.GetPlugin(id).ShouldNotBeNull().Manifest.Version.ShouldBe(new Version(3, 0, 0));
        File.Exists(third).ShouldBeFalse();
        _state.Plugins.Count(p => p.Manifest.Id == id).ShouldBe(1);
        _notifications.ShouldBeEmpty();
        _logger.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "A rebuild that can't be loaded keeps the previous version and notifies")]
    public async Task ReloadFailure_KeepsPreviousVersionAndNotifies()
    {
        const string id = "mythetech.test.local-broken";
        var name = UniqueName("Broken");
        var dllPath = TestPluginBuilder.Build(_binFolder, name, id, new Version(1, 0, 0));
        var service = CreateService();
        await service.AddAsync(dllPath);
        var before = _state.GetPlugin(id).ShouldNotBeNull();

        await File.WriteAllBytesAsync(dllPath, [0x4D, 0x5A, 0x00, 0x13, 0x37, 0xFF, 0x00, 0x42], Ct);

        var expected = new AddNotification($"The local plugin {name} couldn't be reloaded.", Severity.Error);
        await WaitUntilAsync(() => _notifications.Contains(expected));
        _notifications.ShouldContain(expected);
        _state.GetPlugin(id).ShouldBeSameAs(before);
    }

    [Fact(DisplayName = "A change under a content root of the build's static web assets manifest reloads the plugin")]
    public async Task ContentRootChange_ReloadsPlugin()
    {
        const string id = "mythetech.test.local-content";
        var name = UniqueName("Content");
        var wwwroot = Path.Combine(_root, "src", "wwwroot");
        Directory.CreateDirectory(wwwroot);
        var dllPath = TestPluginBuilder.Build(_binFolder, name, id, new Version(1, 0, 0));
        var manifest = new { ContentRoots = new[] { wwwroot + Path.DirectorySeparatorChar }, Root = new { Children = new { } } };
        await File.WriteAllTextAsync(Path.Combine(_binFolder, $"{name}.staticwebassets.runtime.json"), JsonSerializer.Serialize(manifest), Ct);
        var service = CreateService();
        await service.AddAsync(dllPath);
        var before = _state.GetPlugin(id).ShouldNotBeNull();

        await File.WriteAllTextAsync(Path.Combine(wwwroot, "plugin.css"), ".plugin { color: red; }", Ct);

        await WaitUntilAsync(() => !ReferenceEquals(_state.GetPlugin(id), before));
        _state.GetPlugin(id).ShouldNotBeNull().ShouldNotBeSameAs(before);
    }

    [Fact(DisplayName = "LoadSavedAsync loads the saved local plugins when enabled and deletes old copies")]
    public async Task LoadSavedAsync_LoadsSavedEntriesWhenEnabled()
    {
        const string id = "mythetech.test.local-saved";
        var dllPath = TestPluginBuilder.Build(_binFolder, UniqueName("Saved"), id, new Version(1, 0, 0));
        await WriteSavedPathsAsync(dllPath);
        var staleCopy = Path.Combine(_copiesRoot, "Stale", "1");
        Directory.CreateDirectory(staleCopy);
        var service = CreateService(enabled: true);

        await service.LoadSavedAsync();

        _state.GetPlugin(id).ShouldNotBeNull().IsLocal.ShouldBeTrue();
        service.Entries.ShouldBe([new LocalPluginEntry(id, dllPath)]);
        Directory.Exists(staleCopy).ShouldBeFalse();
        ReadSavedPaths().ShouldBe([dllPath]);
    }

    [Fact(DisplayName = "LoadSavedAsync does nothing when local plugins are disabled")]
    public async Task LoadSavedAsync_DoesNothingWhenDisabled()
    {
        const string id = "mythetech.test.local-disabled";
        var dllPath = TestPluginBuilder.Build(_binFolder, UniqueName("Disabled"), id, new Version(1, 0, 0));
        await WriteSavedPathsAsync(dllPath);
        var staleCopy = Path.Combine(_copiesRoot, "Stale", "1");
        Directory.CreateDirectory(staleCopy);
        var service = CreateService(enabled: false);

        await service.LoadSavedAsync();

        _state.GetPlugin(id).ShouldBeNull();
        service.Entries.ShouldBeEmpty();
        Directory.Exists(staleCopy).ShouldBeTrue();
    }

    [Fact(DisplayName = "LoadSavedAsync keeps a saved path that can't be loaded")]
    public async Task LoadSavedAsync_MissingBuild_KeepsSavedPath()
    {
        const string id = "mythetech.test.local-kept";
        var missing = Path.Combine(_root, "gone", "bin", "Gone.dll");
        var dllPath = TestPluginBuilder.Build(_binFolder, UniqueName("Kept"), id, new Version(1, 0, 0));
        await WriteSavedPathsAsync(missing);
        var service = CreateService(enabled: true);
        await service.LoadSavedAsync();

        await service.AddAsync(dllPath);

        ReadSavedPaths().ShouldBe([missing, dllPath]);
    }

    [Fact(DisplayName = "SetEnabledAsync(true) after plugins have loaded loads the saved local plugins")]
    public async Task SetEnabledAsync_AfterPluginsLoaded_LoadsSaved()
    {
        const string id = "mythetech.test.local-enable";
        var dllPath = TestPluginBuilder.Build(_binFolder, UniqueName("Enable"), id, new Version(1, 0, 0));
        await WriteSavedPathsAsync(dllPath);
        _state.PluginsLoaded = true;
        var service = CreateService(enabled: false);

        await service.SetEnabledAsync(true);

        service.IsEnabled.ShouldBeTrue();
        _state.GetPlugin(id).ShouldNotBeNull().IsLocal.ShouldBeTrue();
    }

    [Fact(DisplayName = "SetEnabledAsync(true) before plugins have loaded leaves loading to plugin initialization")]
    public async Task SetEnabledAsync_BeforePluginsLoaded_DoesNotLoad()
    {
        const string id = "mythetech.test.local-early";
        var dllPath = TestPluginBuilder.Build(_binFolder, UniqueName("Early"), id, new Version(1, 0, 0));
        await WriteSavedPathsAsync(dllPath);
        var service = CreateService(enabled: false);

        await service.SetEnabledAsync(true);

        service.IsEnabled.ShouldBeTrue();
        _state.GetPlugin(id).ShouldBeNull();
    }

    [Fact(DisplayName = "InitializePluginsAsync loads local plugins after installed ones and before PluginsLoadingCompleted")]
    public async Task InitializePluginsAsync_LoadsLocalPluginsBeforeLoadingCompleted()
    {
        var local = Substitute.For<ILocalPluginService>();
        _state.SetMessageBus(_bus);
        _state.SetPluginLoader(_loader);
        _state.SetLocalPluginService(local);

        await _state.InitializePluginsAsync(_pluginsDirectory);

        Received.InOrder(() =>
        {
            local.LoadSavedAsync();
            _bus.PublishAsync(Arg.Any<PluginsLoadingCompleted>());
        });
    }

    [Fact(DisplayName = "PluginSettingsConsumer applies the local plugins setting")]
    public async Task PluginSettingsConsumer_AppliesLocalPluginsEnabled()
    {
        var local = Substitute.For<ILocalPluginService>();
        var consumer = new PluginSettingsConsumer(_state, local, NullLogger<PluginSettingsConsumer>.Instance);

        await consumer.Consume(new SettingsModelChanged<PluginSettings>(new PluginSettings { LocalPluginsEnabled = true }));

        await local.Received(1).SetEnabledAsync(true);
    }

    [Fact(DisplayName = "LocalPluginService without the plugin framework is unavailable")]
    public async Task WithoutPluginFramework_IsUnavailable()
    {
        await using var service = new LocalPluginService(_bus, NullLogger<LocalPluginService>.Instance);

        var result = await service.AddAsync(Path.Combine(_binFolder, "Any.dll"));

        service.IsAvailable.ShouldBeFalse();
        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldBe("Local plugins aren't available in this app.");
    }

    [Fact(DisplayName = "UnavailableLocalPluginService.AddAsync fails with fixed copy")]
    public async Task UnavailableLocalPluginService_AddAsync_Fails()
    {
        var service = new UnavailableLocalPluginService();

        var result = await service.AddAsync(Path.Combine(_binFolder, "Any.dll"));

        service.IsAvailable.ShouldBeFalse();
        service.IsEnabled.ShouldBeFalse();
        service.Entries.ShouldBeEmpty();
        result.Succeeded.ShouldBeFalse();
        result.Plugins.ShouldBeEmpty();
        result.Message.ShouldBe("Local plugins aren't available in this app.");
        (await service.RemoveAsync("any")).ShouldBeFalse();
    }

    private LocalPluginService CreateService(bool enabled = true)
    {
        var settings = Substitute.For<ISettingsProvider>();
        settings.GetSettings<PluginSettings>().Returns(new PluginSettings { LocalPluginsEnabled = enabled });
        var service = new LocalPluginService(_bus, _logger, _copiesRoot, _loader, _state, settings);
        _services.Add(service);
        return service;
    }

    private async Task<PluginInfo> InstallAsync(string pluginId, Version version)
    {
        var folder = Path.Combine(_pluginsDirectory, UniqueName("Installed"));
        TestPluginBuilder.Build(folder, UniqueName("Installed"), pluginId, version);
        var plugin = _loader.LoadPluginsFromFolder(folder).ShouldHaveSingleItem();
        await _state.RegisterPluginAsync(plugin);
        return plugin;
    }

    private string SavedPathsFile => Path.Combine(_pluginsDirectory, "local-plugins.json");

    private string[] ReadSavedPaths() => JsonSerializer.Deserialize<string[]>(File.ReadAllText(SavedPathsFile)).ShouldNotBeNull();

    private Task WriteSavedPathsAsync(params string[] paths) =>
        File.WriteAllTextAsync(SavedPathsFile, JsonSerializer.Serialize(paths), Ct);

    private string StagingFolder()
    {
        var folder = Path.Combine(_root, "staging", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + ReloadDeadline;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, Ct);
        }
    }

    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

    private static string UniqueName(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public async ValueTask DisposeAsync()
    {
        foreach (var service in _services)
        {
            await service.DisposeAsync();
        }

        _state.Dispose();
        try { Directory.Delete(_root, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        GC.SuppressFinalize(this);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public ConcurrentQueue<Exception?> Errors { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
                Errors.Enqueue(exception);
        }
    }
}
