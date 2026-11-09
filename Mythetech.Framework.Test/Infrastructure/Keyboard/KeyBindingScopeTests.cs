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
    private readonly KeyBindingSettings _settings = new(Options.Create(new KeyBindingSettingsOptions()));

    private KeyBindingService CreateService(params KeyBindingDefinition[] definitions)
    {
        var options = new KeyBindingRegistrationOptions();
        options.Definitions.AddRange(definitions);

        return new KeyBindingService(
            new KeyBindingRegistry(Options.Create(options)),
            _settings,
            Substitute.For<ISettingsProvider>(),
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
}
