using System.IO.Compression;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
using Mythetech.Framework.Test.Infrastructure.Plugins.TestPlugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"plugin_installer_test_{Guid.NewGuid():N}");
    private readonly string _pluginsDirectory;
    private readonly PluginState _state = new();
    private readonly PluginLoader _loader;
    private readonly PluginInstaller _installer;

    public PluginInstallerTests()
    {
        _pluginsDirectory = Path.Combine(_root, "plugins");
        Directory.CreateDirectory(_pluginsDirectory);
        _state.SetPluginDirectory(_pluginsDirectory);
        _loader = new PluginLoader(NullLogger<PluginLoader>.Instance, _state);
        _installer = new PluginInstaller(_state, _loader, NullLogger<PluginInstaller>.Instance);
    }

    [Fact(DisplayName = "InstallFromStreamAsync registers a zipped plugin from a new install folder")]
    public async Task InstallFromStreamAsync_Zip_RegistersFromNewInstallFolder()
    {
        var name = UniqueName("Zip");
        using var package = BuildPackage(name, "mythetech.test.zip", new Version(1, 0, 0));

        var result = await _installer.InstallFromStreamAsync(package, $"{name}.zip", "Zip");

        result.Succeeded.ShouldBeTrue();
        result.Message.ShouldBe($"Installed {name} 1.0.0.");
        var plugin = _state.GetPlugin("mythetech.test.zip").ShouldNotBeNull();
        result.Plugins.ShouldBe([plugin]);
        var folder = PluginFolder(plugin);
        Path.GetDirectoryName(folder).ShouldBe(Path.Combine(_pluginsDirectory, "Zip"));
        Path.GetFileName(folder).ShouldStartWith(PluginInstallLayout.InstallFolderPrefix);
        IsMarked(folder).ShouldBeFalse();
    }

    [Fact(DisplayName = "An install interrupted before it loads is deleted at the next startup, which keeps the previous install")]
    public async Task InterruptedInstall_NextStartup_KeepsPreviousInstall()
    {
        var name = UniqueName("Interrupted");
        using var v1 = BuildPackage(name, "mythetech.test.interrupted", new Version(1, 0, 0));
        var installed = await _installer.InstallFromStreamAsync(v1, $"{name}.zip", "Interrupted");
        var v1Folder = PluginFolder(installed.Plugins.ShouldHaveSingleItem());
        var partial = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Interrupted");
        TestPluginBuilder.Build(partial, name, "mythetech.test.interrupted", new Version(2, 0, 0));

        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);

        folders.ShouldBe([v1Folder]);
        Directory.Exists(partial).ShouldBeFalse();
    }

    [Fact(DisplayName = "InstallFromStreamAsync of a newer version replaces the plugin and marks the old folder")]
    public async Task InstallFromStreamAsync_Upgrade_ReplacesAndMarksPreviousFolder()
    {
        var name = UniqueName("Upgrade");
        using var v1 = BuildPackage(name, "mythetech.test.upgrade", new Version(1, 0, 0));
        using var v2 = BuildPackage(name, "mythetech.test.upgrade", new Version(2, 0, 0));
        var first = await _installer.InstallFromStreamAsync(v1, $"{name}.zip", "Upgrade");
        var v1Folder = PluginFolder(first.Plugins.ShouldHaveSingleItem());

        var second = await _installer.InstallFromStreamAsync(v2, $"{name}.zip", "Upgrade");

        second.Succeeded.ShouldBeTrue();
        second.Message.ShouldBe($"Updated {name} to 2.0.0.");
        _state.GetPlugin("mythetech.test.upgrade").ShouldNotBeNull().Manifest.Version.ShouldBe(new Version(2, 0, 0));
        IsMarked(v1Folder).ShouldBeTrue();
        IsMarked(PluginFolder(second.Plugins.ShouldHaveSingleItem())).ShouldBeFalse();
    }

    [Fact(DisplayName = "InstallFromStreamAsync over a legacy install marks the legacy folder, which the next startup deletes")]
    public async Task InstallFromStreamAsync_UpgradesLegacyInstall_MarksLegacyFolder()
    {
        var name = UniqueName("Legacy");
        var legacyFolder = Path.Combine(_pluginsDirectory, "Legacy");
        TestPluginBuilder.Build(legacyFolder, name, "mythetech.test.legacy", new Version(1, 0, 0));
        await LoadAtStartupAsync();
        using var v2 = BuildPackage(name, "mythetech.test.legacy", new Version(2, 0, 0));

        var result = await _installer.InstallFromStreamAsync(v2, $"{name}.zip", "mythetech.test.legacy");

        result.Succeeded.ShouldBeTrue();
        IsMarked(legacyFolder).ShouldBeTrue();
        var newFolder = PluginFolder(result.Plugins.ShouldHaveSingleItem());
        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);
        Directory.Exists(legacyFolder).ShouldBeFalse();
        folders.ShouldBe([newFolder]);
    }

    [Fact(DisplayName = "InstallFromStreamAsync of a package without a plugin fails and marks its folder")]
    public async Task InstallFromStreamAsync_PackageWithoutPlugin_FailsAndMarksFolder()
    {
        using var package = ZipFiles(("readme.txt", "no plugin here"u8.ToArray()));

        var result = await _installer.InstallFromStreamAsync(package, "empty.zip", "Empty");

        result.Succeeded.ShouldBeFalse();
        result.Plugins.ShouldBeEmpty();
        result.Message.ShouldBe("That package doesn't contain a plugin.");
        var installFolder = Directory.GetDirectories(Path.Combine(_pluginsDirectory, "Empty")).ShouldHaveSingleItem();
        IsMarked(installFolder).ShouldBeTrue();
    }

    [Fact(DisplayName = "InstallFromStreamAsync that fails an upgrade keeps the previous registration and folder")]
    public async Task InstallFromStreamAsync_FailedUpgrade_KeepsPreviousRegistration()
    {
        var name = UniqueName("FailedUpgrade");
        using var v1 = BuildPackage(name, "mythetech.test.failed-upgrade", new Version(1, 0, 0));
        var first = await _installer.InstallFromStreamAsync(v1, $"{name}.zip", "FailedUpgrade");
        var v1Plugin = first.Plugins.ShouldHaveSingleItem();
        using var broken = ZipFiles(("readme.txt", "no plugin here"u8.ToArray()));

        var result = await _installer.InstallFromStreamAsync(broken, $"{name}.zip", "FailedUpgrade");

        result.Succeeded.ShouldBeFalse();
        _state.GetPlugin("mythetech.test.failed-upgrade").ShouldBeSameAs(v1Plugin);
        IsMarked(PluginFolder(v1Plugin)).ShouldBeFalse();
    }

    [Fact(DisplayName = "InstallFromStreamAsync of something that isn't a zip reports an invalid package and marks the folder")]
    public async Task InstallFromStreamAsync_CorruptZip_FailsAndMarksFolder()
    {
        using var package = new MemoryStream("not a zip"u8.ToArray());

        var result = await _installer.InstallFromStreamAsync(package, "corrupt.zip", "Corrupt");

        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldBe("That download isn't a plugin package.");
        IsMarked(Directory.GetDirectories(Path.Combine(_pluginsDirectory, "Corrupt")).ShouldHaveSingleItem()).ShouldBeTrue();
    }

    [Fact(DisplayName = "InstallFromStreamAsync installs a bare plugin DLL")]
    public async Task InstallFromStreamAsync_BareDll_Installs()
    {
        var name = UniqueName("Bare");
        var dllPath = TestPluginBuilder.Build(StagingFolder(), name, "mythetech.test.bare", new Version(1, 0, 0));
        await using var package = File.OpenRead(dllPath);

        var result = await _installer.InstallFromStreamAsync(package, $"{name}.dll", "Bare");

        result.Succeeded.ShouldBeTrue();
        Path.GetFileName(result.Plugins.ShouldHaveSingleItem().SourcePath).ShouldBe($"{name}.dll");
    }

    [Fact(DisplayName = "Removing a plugin and reinstalling it in one session leaves the new install loadable at startup")]
    public async Task RemoveThenReinstall_SameSession_NewInstallSurvivesStartup()
    {
        var name = UniqueName("Reinstall");
        using var first = BuildPackage(name, "mythetech.test.reinstall", new Version(1, 0, 0));
        using var again = BuildPackage(name, "mythetech.test.reinstall", new Version(1, 0, 0));
        await _installer.InstallFromStreamAsync(first, $"{name}.zip", "Reinstall");
        await _state.RemovePluginAsync("mythetech.test.reinstall");

        var result = await _installer.InstallFromStreamAsync(again, $"{name}.zip", "Reinstall");

        result.Succeeded.ShouldBeTrue();
        _state.GetPlugin("mythetech.test.reinstall").ShouldNotBeNull();
        var newFolder = PluginFolder(result.Plugins.ShouldHaveSingleItem());
        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);
        folders.ShouldBe([newFolder]);
        Directory.Exists(newFolder).ShouldBeTrue();
    }

    [Fact(DisplayName = "Removing a legacy plugin and reinstalling it under the same key leaves the new install loadable at startup")]
    public async Task RemoveLegacyThenReinstall_SameKey_NewInstallSurvivesStartup()
    {
        var name = UniqueName("LegacyReinstall");
        var legacyFolder = Path.Combine(_pluginsDirectory, "LegacyReinstall");
        TestPluginBuilder.Build(legacyFolder, name, "mythetech.test.legacy-reinstall", new Version(1, 0, 0));
        await LoadAtStartupAsync();
        await _state.RemovePluginAsync("mythetech.test.legacy-reinstall");
        using var package = BuildPackage(name, "mythetech.test.legacy-reinstall", new Version(1, 0, 0));

        var result = await _installer.InstallFromStreamAsync(package, $"{name}.zip", "LegacyReinstall");

        result.Succeeded.ShouldBeTrue();
        var newFolder = PluginFolder(result.Plugins.ShouldHaveSingleItem());
        Path.GetDirectoryName(newFolder).ShouldBe(legacyFolder);
        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);
        folders.ShouldBe([newFolder]);
        Directory.Exists(newFolder).ShouldBeTrue();
        File.Exists(Path.Combine(legacyFolder, $"{name}.dll")).ShouldBeFalse();
    }

    [Fact(DisplayName = "RemovePluginAsync marks the plugin's folder instead of deleting it and still deletes its storage")]
    public async Task RemovePluginAsync_MarksFolderInsteadOfDeleting()
    {
        var storage = Substitute.For<IPluginStorageFactory>();
        _state.SetStorageFactory(storage);
        var name = UniqueName("Remove");
        using var package = BuildPackage(name, "mythetech.test.remove", new Version(1, 0, 0));
        var installed = await _installer.InstallFromStreamAsync(package, $"{name}.zip", "Remove");
        var folder = PluginFolder(installed.Plugins.ShouldHaveSingleItem());

        var removed = await _state.RemovePluginAsync("mythetech.test.remove");

        removed.ShouldBeTrue();
        _state.GetPlugin("mythetech.test.remove").ShouldBeNull();
        File.Exists(Path.Combine(folder, $"{name}.dll")).ShouldBeTrue();
        IsMarked(folder).ShouldBeTrue();
        await storage.Received(1).DeletePluginDataAsync("mythetech.test.remove");
    }

    [Fact(DisplayName = "ImportFromFileAsync of the registered version replaces it without throwing")]
    public async Task ImportFromFileAsync_SameVersionAsRegistered_ReplacesWithoutThrowing()
    {
        var name = UniqueName("Import");
        using var package = BuildPackage(name, "mythetech.test.import", new Version(1, 0, 0));
        await _installer.InstallFromStreamAsync(package, $"{name}.zip", "Import");
        var dllPath = TestPluginBuilder.Build(StagingFolder(), name, "mythetech.test.import", new Version(1, 0, 0));

        var result = await _installer.ImportFromFileAsync(dllPath);

        result.Succeeded.ShouldBeTrue();
        result.Message.ShouldBe($"Updated {name} to 1.0.0.");
        _state.GetPlugin("mythetech.test.import").ShouldNotBeNull().SourcePath.ShouldBe(dllPath);
    }

    [Fact(DisplayName = "ImportFromFileAsync of a file without a plugin fails")]
    public async Task ImportFromFileAsync_NoPlugin_Fails()
    {
        var path = Path.Combine(StagingFolder(), "NotAPlugin.dll");
        await File.WriteAllTextAsync(path, "not an assembly", TestContext.Current.CancellationToken);

        var result = await _installer.ImportFromFileAsync(path);

        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldBe("No plugin was found in that file.");
    }

    [Fact(DisplayName = "InstallFromUrlAsync downloads the package and installs it")]
    public async Task InstallFromUrlAsync_DownloadsAndInstalls()
    {
        var name = UniqueName("Download");
        using var package = BuildPackage(name, "mythetech.test.download", new Version(1, 0, 0));
        var bytes = package.ToArray();
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }));
        var installer = new PluginInstaller(_state, _loader, NullLogger<PluginInstaller>.Instance, httpClient: client);

        var result = await installer.InstallFromUrlAsync($"https://example.com/{name}-latest.zip", "Download", TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        _state.GetPlugin("mythetech.test.download").ShouldNotBeNull();
    }

    [Fact(DisplayName = "InstallFromUrlAsync reports a failed download without creating an install folder")]
    public async Task InstallFromUrlAsync_DownloadFails_ReportsConnectionProblem()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        var installer = new PluginInstaller(_state, _loader, NullLogger<PluginInstaller>.Instance, httpClient: client);

        var result = await installer.InstallFromUrlAsync("https://example.com/missing.zip", "Missing", TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldBe("Couldn't download the plugin. Check your connection and try again.");
        Directory.Exists(Path.Combine(_pluginsDirectory, "Missing")).ShouldBeFalse();
    }

    [Fact(DisplayName = "InstallFromUrlAsync without an HttpClient reports that downloads aren't available")]
    public async Task InstallFromUrlAsync_NoHttpClient_ReportsUnavailable()
    {
        var result = await _installer.InstallFromUrlAsync("https://example.com/plugin.zip", "NoClient", TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldBe("Plugin downloads aren't available in this app.");
    }

    private async Task LoadAtStartupAsync()
    {
        foreach (var plugin in _loader.LoadPluginsFromDirectory(_pluginsDirectory))
        {
            await _state.RegisterPluginAsync(plugin);
        }
    }

    private MemoryStream BuildPackage(string assemblyName, string pluginId, Version version)
    {
        var dllPath = TestPluginBuilder.Build(StagingFolder(), assemblyName, pluginId, version);
        return ZipFiles((Path.GetFileName(dllPath), File.ReadAllBytes(dllPath)));
    }

    private static MemoryStream ZipFiles(params (string Name, byte[] Content)[] files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                using var entry = archive.CreateEntry(name).Open();
                entry.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private string StagingFolder()
    {
        var folder = Path.Combine(_root, "staging", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string PluginFolder(PluginInfo plugin) => Path.GetDirectoryName(plugin.SourcePath.ShouldNotBeNull())!;

    private static bool IsMarked(string folder) => File.Exists(Path.Combine(folder, PluginInstallLayout.RemovedMarkerFileName));

    private static string UniqueName(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public void Dispose()
    {
        _state.Dispose();
        try { Directory.Delete(_root, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
