using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.Environment;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Components;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
using Mythetech.Framework.Infrastructure.Plugins.Local;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginManagementDialogTests : BunitContext
{
    private const string EntryId = "notes";
    private const string EntryUri = "https://example.com/notes.zip";
    private const string RegistryLoadFailureMessage = "Couldn't load the plugin registry. Check your connection and try again.";
    private const string AddLocalPluginText = "Add local plugin…";
    private const string LocalDllPath = "/src/Notes/bin/Debug/net11.0/Notes.dll";

    private readonly IPluginRegistryService _registryService = Substitute.For<IPluginRegistryService>();
    private readonly IPluginInstaller _installer = Substitute.For<IPluginInstaller>();
    private readonly ILocalPluginService _localPlugins = Substitute.For<ILocalPluginService>();
    private readonly IFileOpenService _fileOpenService = Substitute.For<IFileOpenService>();
    private readonly PluginState _state = new();
    private IRenderedComponent<MudPopoverProvider>? _popovers;

    public PluginManagementDialogTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var runtime = Substitute.For<IRuntimeEnvironment>();
        runtime.Name.Returns("Development");
        runtime.Platform.Returns(Platform.Desktop);

        _localPlugins.Entries.Returns([]);

        Services.AddSingleton(_state);
        Services.AddSingleton(runtime);
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(_registryService);
        Services.AddSingleton(_installer);
        Services.AddSingleton(_localPlugins);
    }

    [Fact(DisplayName = "Installing a registry plugin shows the installer's message")]
    public async Task Install_ShowsInstallerMessage()
    {
        _registryService.FetchRegistryAsync(Arg.Any<CancellationToken>()).Returns(new PluginRegistry
        {
            Plugins = [new RegistryPluginEntry { Id = EntryId, Name = "Notes", Version = "1.0.0", Uri = EntryUri }]
        });
        _installer.InstallFromUrlAsync(EntryUri, EntryId, Arg.Any<CancellationToken>())
            .Returns(new PluginInstallResult(true, [], "Installed Notes 1.0.0."));

        var host = await OpenDialogAsync();
        await host.Find("a:contains('Available')").ClickAsync();
        await host.Find("button:contains('Install')").ClickAsync();

        await _installer.Received(1).InstallFromUrlAsync(EntryUri, EntryId, Arg.Any<CancellationToken>());
        var snackbar = Services.GetRequiredService<ISnackbar>();
        host.WaitForAssertion(() => snackbar.ShownSnackbars.Select(x => x.Message).ShouldContain("Installed Notes 1.0.0."));
    }

    [Fact(DisplayName = "A registry load failure shows fixed copy, not the exception text")]
    public async Task RegistryLoadFailure_ShowsFixedMessage()
    {
        _registryService.FetchRegistryAsync(Arg.Any<CancellationToken>())
            .Returns<PluginRegistry?>(_ => throw new HttpRequestException("secret-host.internal refused"));

        var host = await OpenDialogAsync();

        host.WaitForAssertion(() => host.Markup.ShouldContain(RegistryLoadFailureMessage));
        host.Markup.ShouldNotContain("secret-host.internal");
    }

    [Fact(DisplayName = "With local plugins enabled, the Installed tab offers Add local plugin")]
    public async Task PluginManagementDialog_LocalEnabled_ShowsAddLocalPlugin()
    {
        Services.AddSingleton(_fileOpenService);
        EnableLocalPlugins();
        RegistryReturns();

        var host = await OpenDialogAsync();

        host.WaitForAssertion(() => host.FindAll($"button:contains('{AddLocalPluginText}')").ShouldHaveSingleItem());
    }

    [Fact(DisplayName = "Without local plugin support or a file picker, the dialog renders and hides Add local plugin")]
    public async Task PluginManagementDialog_LocalUnavailable_HidesAddLocalPlugin()
    {
        // No IFileOpenService is registered, as in a Blazor Server host.
        _localPlugins.IsAvailable.Returns(false);
        _localPlugins.IsEnabled.Returns(true);
        RegistryReturns();

        var host = await OpenDialogAsync();

        host.WaitForAssertion(() => host.Markup.ShouldContain("Installed (0)"));
        host.Markup.ShouldNotContain("Add local plugin");
    }

    [Fact(DisplayName = "With local plugins turned off, the Installed tab hides Add local plugin")]
    public async Task LocalDisabled_HidesAddLocalPlugin()
    {
        _localPlugins.IsAvailable.Returns(true);
        _localPlugins.IsEnabled.Returns(false);
        RegistryReturns();

        var host = await OpenDialogAsync();

        host.WaitForAssertion(() => host.Markup.ShouldContain("Installed (0)"));
        host.Markup.ShouldNotContain("Add local plugin");
    }

    [Fact(DisplayName = "Add local plugin adds the chosen DLL and shows the result")]
    public async Task AddLocalPlugin_AddsChosenDllAndShowsMessage()
    {
        Services.AddSingleton(_fileOpenService);
        EnableLocalPlugins();
        RegistryReturns();
        _fileOpenService.OpenFileAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<FileFilter[]?>())
            .Returns([LocalDllPath]);
        _localPlugins.AddAsync(LocalDllPath).Returns(new PluginInstallResult(true, [], "Added local plugin Notes."));

        var host = await OpenDialogAsync();
        await host.Find($"button:contains('{AddLocalPluginText}')").ClickAsync();

        await _fileOpenService.Received(1).OpenFileAsync(
            "Select Plugin DLL",
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Is<FileFilter[]?>(filters => filters != null
                && filters.Length == 1
                && filters[0].Name == "Plugin DLLs"
                && filters[0].Extensions.SequenceEqual(new[] { "*.dll" })));
        await _localPlugins.Received(1).AddAsync(LocalDllPath);
        var snackbar = Services.GetRequiredService<ISnackbar>();
        host.WaitForAssertion(() => snackbar.ShownSnackbars.Select(x => x.Message).ShouldContain("Added local plugin Notes."));
    }

    [Fact(DisplayName = "A local plugin's row shows a Local badge with its path, and its menu offers Remove instead of Delete")]
    public async Task LocalPlugin_ShowsLocalBadgeAndRemove()
    {
        Services.AddSingleton(_fileOpenService);
        EnableLocalPlugins();
        RegistryReturns();
        await _state.RegisterPluginAsync(Plugin(EntryId, "1.0.0", isLocal: true));
        _localPlugins.Entries.Returns([new LocalPluginEntry(EntryId, LocalDllPath)]);

        var host = await OpenDialogAsync();
        host.WaitForAssertion(() => host.FindAll(".mythetech-badge").Select(b => b.TextContent.Trim()).ShouldContain("Local"));
        host.FindComponents<MudTooltip>().Select(t => t.Instance.Text).ShouldContain(LocalDllPath);

        await host.Find(".mud-table-body .mud-menu button").ClickAsync();
        var popovers = _popovers.ShouldNotBeNull();
        popovers.WaitForAssertion(() => MenuItemTexts(popovers).ShouldContain("Remove"));
        MenuItemTexts(popovers).ShouldNotContain("Delete");

        await popovers.FindAll(".mud-menu-item")
            .First(item => item.QuerySelector(".mud-menu-item-text")?.TextContent.Trim() == "Remove")
            .ClickAsync();

        await _localPlugins.Received(1).RemoveAsync(EntryId);
    }

    [Fact(DisplayName = "An installed plugin's menu still offers Delete")]
    public async Task InstalledPlugin_OffersDelete()
    {
        RegistryReturns();
        await _state.RegisterPluginAsync(Plugin(EntryId, "1.0.0", isLocal: false));

        var host = await OpenDialogAsync();
        await host.Find(".mud-table-body .mud-menu button").ClickAsync();

        var popovers = _popovers.ShouldNotBeNull();
        popovers.WaitForAssertion(() => MenuItemTexts(popovers).ShouldContain("Delete"));
        MenuItemTexts(popovers).ShouldNotContain("Remove");
    }

    [Fact(DisplayName = "A registry entry for a local plugin is listed with a Local badge instead of Install or Upgrade")]
    public async Task AvailableEntryForLocalPlugin_ShowsLocalBadge()
    {
        RegistryReturns(new RegistryPluginEntry { Id = EntryId, Name = "Notes", Version = "1.0.0", Uri = EntryUri });
        await _state.RegisterPluginAsync(Plugin(EntryId, "1.0.0", isLocal: true));

        var host = await OpenDialogAsync();
        await host.Find("a:contains('Available')").ClickAsync();

        host.WaitForAssertion(() => host.Markup.ShouldContain("Available (1)"));
        host.FindAll(".mud-table-body .mythetech-badge").Select(b => b.TextContent.Trim()).ShouldContain("Local");
        host.FindAll("button:contains('Install')").ShouldBeEmpty();
        host.FindAll("button:contains('Upgrade')").ShouldBeEmpty();
    }

    private static List<string> MenuItemTexts(IRenderedComponent<MudPopoverProvider> popovers) =>
        popovers.FindAll(".mud-menu-item .mud-menu-item-text").Select(text => text.TextContent.Trim()).ToList();

    private void EnableLocalPlugins()
    {
        _localPlugins.IsAvailable.Returns(true);
        _localPlugins.IsEnabled.Returns(true);
    }

    private void RegistryReturns(params RegistryPluginEntry[] entries)
    {
        _registryService.FetchRegistryAsync(Arg.Any<CancellationToken>()).Returns(new PluginRegistry { Plugins = [.. entries] });
    }

    private static PluginInfo Plugin(string id, string version, bool isLocal)
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns(id);
        manifest.Name.Returns("Notes");
        manifest.Developer.Returns("Mythetech");
        manifest.Version.Returns(Version.Parse(version));
        return new PluginInfo { Manifest = manifest, Assembly = Assembly.GetExecutingAssembly(), IsLocal = isLocal };
    }

    private async Task<IRenderedComponent<MudDialogProvider>> OpenDialogAsync()
    {
        var host = Render<MudDialogProvider>();
        _popovers = Render<MudPopoverProvider>();
        Render<MudSnackbarProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        await host.InvokeAsync(() => dialogService.ShowAsync<PluginManagementDialog>("Plugin Management"));
        return host;
    }
}
