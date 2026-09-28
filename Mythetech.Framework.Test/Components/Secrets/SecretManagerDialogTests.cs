using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Input;
using Mythetech.Framework.Components.Secrets;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Secrets;

public class SecretManagerDialogTests : BunitContext
{
    private readonly SecretManagerSettings _settings = new();
    private readonly ISettingsProvider _settingsProvider = Substitute.For<ISettingsProvider>();
    private readonly SecretManagerState _state;
    private readonly ISecretManager _keychain = NamedManager("macOS Keychain");
    private readonly ISecretManager _onePassword = NamedManager("1Password CLI");

    public SecretManagerDialogTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _state = new SecretManagerState(_settings, _settingsProvider);
        _state.RegisterManager(_keychain);
        _state.RegisterManager(_onePassword);
        Services.AddSingleton(_state);
    }

    [Fact(DisplayName = "Choosing a manager in the dialog makes it active and saves the choice")]
    public async Task ChoosingManager_MakesItActiveAndSavesChoice()
    {
        var host = Render<MudDialogProvider>();
        Render<MudPopoverProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        await host.InvokeAsync(() => dialogService.ShowAsync<SecretManagerDialog>("Secret Manager"));

        var managerSelect = host.FindComponent<MtSelect<string>>();
        await host.InvokeAsync(() => managerSelect.Instance.ValueChanged.InvokeAsync(_onePassword.Name));

        _state.CurrentManager.ShouldBe(_onePassword);
        _settings.ActiveManagerName.ShouldBe(_onePassword.Name);
        await _settingsProvider.Received(1).NotifySettingsChangedAsync(_settings);
    }

    private static ISecretManager NamedManager(string name)
    {
        var manager = Substitute.For<ISecretManager>();
        manager.Name.Returns(name);
        return manager;
    }
}
