using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MudBlazor.Utilities;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Keyboard;

public class KeyBindingScopeTests
{
    private readonly ISettingsProvider _settingsProvider = Substitute.For<ISettingsProvider>();
    private readonly KeyBindingSettings _settings = new(Options.Create(new KeyBindingSettingsOptions()));

    private KeyBindingService CreateService(params KeyBindingDefinition[] definitions) => CreateService([], definitions);

    private KeyBindingService CreateService(IEnumerable<ReservedKeyBinding> reserved, params KeyBindingDefinition[] definitions)
    {
        var options = new KeyBindingRegistrationOptions();
        options.Definitions.AddRange(definitions);
        options.Reserved.AddRange(reserved);

        return new KeyBindingService(
            new KeyBindingRegistry(Options.Create(options)),
            _settings,
            _settingsProvider,
            Substitute.For<IMessageBus>(),
            NullLogger<KeyBindingService>.Instance);
    }

    private static KeyBindingDefinition Definition(string id, KeyBinding? binding, KeyBindingScope scope)
        => new(id, id, null, "General", binding, (_, _) => Task.CompletedTask, scope);

    [Fact(DisplayName = "The same key can be bound globally and in the editor without a conflict")]
    public void Same_key_across_scopes_does_not_conflict()
    {
        var service = CreateService(
            Definition("global-find", KeyBinding.CtrlOrCmd(JsKey.KeyF), KeyBindingScope.Global),
            Definition("editor-find", null, KeyBindingScope.Editor));

        service.TryFindConflict("editor-find", KeyBinding.CtrlOrCmd(JsKey.KeyF), out var conflictingId).ShouldBeFalse();
        conflictingId.ShouldBeNull();
    }

    [Fact(DisplayName = "Two editor actions on one key still conflict")]
    public void Same_key_in_editor_scope_conflicts()
    {
        var service = CreateService(
            Definition("find-next", KeyBinding.CtrlOrCmd(JsKey.KeyG), KeyBindingScope.Editor),
            Definition("go-to-line", null, KeyBindingScope.Editor));

        service.TryFindConflict("go-to-line", KeyBinding.CtrlOrCmd(JsKey.KeyG), out var conflictingId).ShouldBeTrue();
        conflictingId.ShouldBe("find-next");
    }

    [Fact(DisplayName = "Reassigning an editor action to a key a global action holds leaves the global binding bound")]
    public async Task Reassign_does_not_unbind_other_scope()
    {
        var service = CreateService(
            Definition("global-find", KeyBinding.CtrlOrCmd(JsKey.KeyF), KeyBindingScope.Global),
            Definition("editor-find", null, KeyBindingScope.Editor));

        await service.ReassignAsync("editor-find", KeyBinding.CtrlOrCmd(JsKey.KeyF));

        service.GetBinding("editor-find").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyF));
        service.GetBinding("global-find").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyF));
    }

    [Fact(DisplayName = "GetAll by scope returns only that scope's actions")]
    public void GetAll_filters_by_scope()
    {
        var service = CreateService(
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS), KeyBindingScope.Global),
            Definition("find-next", KeyBinding.CtrlOrCmd(JsKey.KeyG), KeyBindingScope.Editor));

        service.GetAll(KeyBindingScope.Editor).Select(b => b.Id).ShouldBe(["find-next"]);
        service.GetAll(KeyBindingScope.Global).Select(b => b.Id).ShouldBe(["save"]);
        service.GetAll(KeyBindingScope.Editor).Single().Scope.ShouldBe(KeyBindingScope.Editor);
    }

    [Fact(DisplayName = "Assigning a shortcut reserved in the action's scope changes nothing and does not persist")]
    public async Task Assigning_a_reserved_shortcut_is_ignored()
    {
        var service = CreateService(
            [new ReservedKeyBinding(KeyBindingScope.Editor, KeyBinding.CtrlOrCmd(JsKey.KeyK))],
            Definition("find", KeyBinding.CtrlOrCmd(JsKey.KeyF), KeyBindingScope.Editor));

        await service.SetBindingAsync("find", KeyBinding.CtrlOrCmd(JsKey.KeyK));

        service.GetBinding("find").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyF));
        _settings.Overrides.ShouldBeEmpty();
        await _settingsProvider.DidNotReceive().NotifySettingsChangedAsync(Arg.Any<SettingsBase>());
    }

    [Fact(DisplayName = "Reassigning to a reserved shortcut leaves the action and the one holding the shortcut as they were")]
    public async Task Reassigning_to_a_reserved_shortcut_is_ignored()
    {
        // An override saved before the shortcut was reserved still holds it.
        var held = SerializedKeyBinding.FromBinding(KeyBinding.CtrlOrCmd(JsKey.KeyK));
        _settings.Overrides["find-next"] = held;
        var service = CreateService(
            [new ReservedKeyBinding(KeyBindingScope.Editor, KeyBinding.CtrlOrCmd(JsKey.KeyK))],
            Definition("find-next", KeyBinding.CtrlOrCmd(JsKey.KeyG), KeyBindingScope.Editor),
            Definition("go-to-line", null, KeyBindingScope.Editor));

        await service.ReassignAsync("go-to-line", KeyBinding.CtrlOrCmd(JsKey.KeyK));

        service.GetBinding("go-to-line").ShouldBeNull();
        _settings.Overrides["find-next"].ShouldBe(held);
        await _settingsProvider.DidNotReceive().NotifySettingsChangedAsync(Arg.Any<SettingsBase>());
    }

    [Fact(DisplayName = "A shortcut reserved in one scope can still be assigned to an action in another")]
    public async Task A_shortcut_reserved_in_another_scope_can_be_assigned()
    {
        var service = CreateService(
            [new ReservedKeyBinding(KeyBindingScope.Editor, KeyBinding.CtrlOrCmd(JsKey.KeyK))],
            Definition("palette", null, KeyBindingScope.Global));

        await service.SetBindingAsync("palette", KeyBinding.CtrlOrCmd(JsKey.KeyK));

        service.GetBinding("palette").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyK));
    }
}
