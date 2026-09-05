using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MudBlazor.Utilities;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Keyboard;

public class KeyBindingServiceMutationTests
{
    private readonly ISettingsProvider _settingsProvider = Substitute.For<ISettingsProvider>();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly KeyBindingSettings _settings = new(Options.Create(new KeyBindingSettingsOptions()));

    private KeyBindingService CreateService(params KeyBindingDefinition[] definitions)
    {
        var options = new KeyBindingRegistrationOptions();
        options.Definitions.AddRange(definitions);

        return new KeyBindingService(
            new KeyBindingRegistry(Options.Create(options)),
            _settings,
            _settingsProvider,
            _bus,
            NullLogger<KeyBindingService>.Instance);
    }

    private static KeyBindingDefinition Definition(string id, KeyBinding? defaultBinding)
        => new(id, id, null, "General", defaultBinding, (_, _) => Task.CompletedTask);

    [Fact(DisplayName = "Assigning a binding to an action that ships unbound stores an override")]
    public async Task Assign_to_unbound_action_stores_override()
    {
        var service = CreateService(Definition("duplicate", null));

        await service.SetBindingAsync("duplicate", KeyBinding.CtrlOrCmd(JsKey.KeyD));

        service.GetBinding("duplicate").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyD));
        service.GetAll().Single().IsCustomized.ShouldBeTrue();
    }

    [Fact(DisplayName = "Assigning the default binding clears the override instead of storing it")]
    public async Task Assigning_default_clears_override()
    {
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));
        await service.SetBindingAsync("save", KeyBinding.CtrlOrCmd(JsKey.KeyD));

        await service.SetBindingAsync("save", KeyBinding.CtrlOrCmd(JsKey.KeyS));

        _settings.Overrides.ShouldNotContainKey("save");
        service.GetAll().Single().IsCustomized.ShouldBeFalse();
    }

    [Fact(DisplayName = "Unbinding a default-bound action stores an explicit null override")]
    public async Task Unbind_default_bound_stores_null()
    {
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        await service.UnbindAsync("save");

        _settings.Overrides["save"].ShouldBeNull();
        service.GetBinding("save").ShouldBeNull();
        service.GetAll().Single().IsCustomized.ShouldBeTrue();
    }

    [Fact(DisplayName = "Unbinding an action that ships unbound clears the override rather than storing null")]
    public async Task Unbind_ships_unbound_clears_override()
    {
        var service = CreateService(Definition("duplicate", null));
        await service.SetBindingAsync("duplicate", KeyBinding.CtrlOrCmd(JsKey.KeyD));

        await service.UnbindAsync("duplicate");

        _settings.Overrides.ShouldNotContainKey("duplicate");
        service.GetAll().Single().IsCustomized.ShouldBeFalse();
    }

    [Fact(DisplayName = "Reset removes the override and returns a default-bound action to its default")]
    public async Task Reset_returns_to_default()
    {
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));
        await service.SetBindingAsync("save", KeyBinding.CtrlOrCmd(JsKey.KeyD));

        await service.ResetAsync("save");

        service.GetBinding("save").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS));
    }

    [Fact(DisplayName = "Reset returns an action that ships unbound to unbound")]
    public async Task Reset_returns_to_unbound()
    {
        var service = CreateService(Definition("duplicate", null));
        await service.SetBindingAsync("duplicate", KeyBinding.CtrlOrCmd(JsKey.KeyD));

        await service.ResetAsync("duplicate");

        service.GetBinding("duplicate").ShouldBeNull();
        service.GetAll().Single().IsCustomized.ShouldBeFalse();
    }

    [Fact(DisplayName = "ResetAll clears every override")]
    public async Task ResetAll_clears_everything()
    {
        var service = CreateService(
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)),
            Definition("duplicate", null));
        await service.SetBindingAsync("save", KeyBinding.CtrlOrCmd(JsKey.KeyD));
        await service.SetBindingAsync("duplicate", KeyBinding.CtrlOrCmd(JsKey.KeyU));

        await service.ResetAllAsync();

        _settings.Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Every mutation persists through the settings provider")]
    public async Task Mutations_persist()
    {
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        await service.SetBindingAsync("save", KeyBinding.CtrlOrCmd(JsKey.KeyD));

        await _settingsProvider.Received(1).NotifySettingsChangedAsync(_settings);
    }

    [Fact(DisplayName = "Mutating an unknown id does nothing and does not persist")]
    public async Task Unknown_id_is_a_no_op()
    {
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        await service.SetBindingAsync("missing", KeyBinding.CtrlOrCmd(JsKey.KeyD));

        _settings.Overrides.ShouldBeEmpty();
        await _settingsProvider.DidNotReceive().NotifySettingsChangedAsync(Arg.Any<SettingsBase>());
    }
}
