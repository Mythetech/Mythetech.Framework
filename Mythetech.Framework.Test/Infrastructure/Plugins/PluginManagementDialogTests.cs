using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.Environment;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Components;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginManagementDialogTests : BunitContext
{
    private const string EntryId = "notes";
    private const string EntryUri = "https://example.com/notes.zip";
    private const string RegistryLoadFailureMessage = "Couldn't load the plugin registry. Check your connection and try again.";

    private readonly IPluginRegistryService _registryService = Substitute.For<IPluginRegistryService>();
    private readonly IPluginInstaller _installer = Substitute.For<IPluginInstaller>();

    public PluginManagementDialogTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var runtime = Substitute.For<IRuntimeEnvironment>();
        runtime.Name.Returns("Development");
        runtime.Platform.Returns(Platform.Desktop);

        Services.AddSingleton(new PluginState());
        Services.AddSingleton(runtime);
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(_registryService);
        Services.AddSingleton(_installer);
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

    private async Task<IRenderedComponent<MudDialogProvider>> OpenDialogAsync()
    {
        var host = Render<MudDialogProvider>();
        Render<MudPopoverProvider>();
        Render<MudSnackbarProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        await host.InvokeAsync(() => dialogService.ShowAsync<PluginManagementDialog>("Plugin Management"));
        return host;
    }
}
