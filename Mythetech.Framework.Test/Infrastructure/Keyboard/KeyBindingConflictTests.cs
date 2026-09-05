using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MudBlazor.Utilities;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Keyboard;

public class KeyBindingConflictTests
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

    private static KeyBindingDefinition Definition(string id, KeyBinding? defaultBinding, string category = "General")
        => new(id, id, null, category, defaultBinding, (_, _) => Task.CompletedTask);

    [Fact(DisplayName = "A binding already held by another action is reported as a conflict")]
    public void Finds_conflict_across_categories()
    {
        var service = CreateService(
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS), "Edit"),
            Definition("search", KeyBinding.CtrlOrCmd(JsKey.KeyS), "Search"));

        var found = service.TryFindConflict("search", KeyBinding.CtrlOrCmd(JsKey.KeyS), out var conflictingId);

        found.ShouldBeTrue();
        conflictingId.ShouldBe("save");
    }

    [Fact(DisplayName = "An action's own binding is not a conflict with itself")]
    public void Ignores_self()
    {
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        service.TryFindConflict("save", KeyBinding.CtrlOrCmd(JsKey.KeyS), out var conflictingId).ShouldBeFalse();
        conflictingId.ShouldBeNull();
    }

    [Fact(DisplayName = "Unbound actions never conflict")]
    public void Unbound_actions_do_not_conflict()
    {
        var service = CreateService(
            Definition("duplicate", null),
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        service.TryFindConflict("save", KeyBinding.CtrlOrCmd(JsKey.KeyD), out _).ShouldBeFalse();
    }

    [Fact(DisplayName = "Reassign takes the binding and leaves the previous holder unbound")]
    public async Task Reassign_unbinds_previous_holder()
    {
        var service = CreateService(
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)),
            Definition("search", null));

        await service.ReassignAsync("search", KeyBinding.CtrlOrCmd(JsKey.KeyS));

        service.GetBinding("search").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS));
        service.GetBinding("save").ShouldBeNull();
    }

    [Fact(DisplayName = "Reassign persists both changes in a single settings notification")]
    public async Task Reassign_persists_once()
    {
        var service = CreateService(
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)),
            Definition("search", null));

        await service.ReassignAsync("search", KeyBinding.CtrlOrCmd(JsKey.KeyS));

        await _settingsProvider.Received(1).NotifySettingsChangedAsync(_settings);
    }

    [Fact(DisplayName = "Reassign with no conflict behaves like a plain assignment")]
    public async Task Reassign_without_conflict()
    {
        var service = CreateService(Definition("search", null));

        await service.ReassignAsync("search", KeyBinding.CtrlOrCmd(JsKey.KeyF));

        service.GetBinding("search").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyF));
    }

    [Fact(DisplayName = "Reassign from unbound action removes override entirely, not just nulls it")]
    public async Task Reassign_from_unbound_action_removes_override()
    {
        var service = CreateService(
            Definition("duplicate", null),
            Definition("search", null));

        await service.SetBindingAsync("duplicate", KeyBinding.CtrlOrCmd(JsKey.KeyD));
        await service.ReassignAsync("search", KeyBinding.CtrlOrCmd(JsKey.KeyD));

        service.GetBinding("search").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyD));
        service.GetBinding("duplicate").ShouldBeNull();
        _settings.Overrides.ShouldNotContainKey("duplicate");
        service.GetAll().First(r => r.Id == "duplicate").IsCustomized.ShouldBeFalse();
    }
}
