using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MudBlazor.Utilities;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Keyboard;

public class KeyBindingServiceTests
{
    private readonly ISettingsProvider _settingsProvider = Substitute.For<ISettingsProvider>();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly KeyBindingSettings _settings = new(Options.Create(new KeyBindingSettingsOptions()));

    private KeyBindingService CreateService(params KeyBindingDefinition[] definitions)
    {
        var options = new KeyBindingRegistrationOptions();
        options.Definitions.AddRange(definitions);
        var registry = new KeyBindingRegistry(Options.Create(options));

        return new KeyBindingService(
            registry,
            _settings,
            _settingsProvider,
            _bus,
            NullLogger<KeyBindingService>.Instance);
    }

    private static KeyBindingDefinition Definition(
        string id,
        KeyBinding? defaultBinding,
        string category = "General")
        => new(id, id, null, category, defaultBinding, (_, _) => Task.CompletedTask);

    [Fact(DisplayName = "A default-bound action with no override resolves to its default")]
    public void Default_bound_resolves_to_default()
    {
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        var resolved = service.GetAll().Single();

        resolved.CurrentBinding.ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS));
        resolved.IsCustomized.ShouldBeFalse();
    }

    [Fact(DisplayName = "An action declared with no default resolves to unbound and uncustomized")]
    public void Ships_unbound_resolves_to_null()
    {
        var service = CreateService(Definition("duplicate", null));

        var resolved = service.GetAll().Single();

        resolved.CurrentBinding.ShouldBeNull();
        resolved.IsCustomized.ShouldBeFalse();
    }

    [Fact(DisplayName = "An override replaces the default and marks the action customized")]
    public void Override_replaces_default()
    {
        _settings.Overrides["save"] = SerializedKeyBinding.FromBinding(KeyBinding.CtrlOrCmd(JsKey.KeyD));
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        var resolved = service.GetAll().Single();

        resolved.CurrentBinding.ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyD));
        resolved.IsCustomized.ShouldBeTrue();
    }

    [Fact(DisplayName = "A null override means explicitly unbound, not missing")]
    public void Null_override_means_unbound()
    {
        _settings.Overrides["save"] = null;
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        var resolved = service.GetAll().Single();

        resolved.CurrentBinding.ShouldBeNull();
        resolved.IsCustomized.ShouldBeTrue();
    }

    [Fact(DisplayName = "An override with an unrecognized key resolves to unbound")]
    public void Stale_override_resolves_to_unbound()
    {
        _settings.Overrides["save"] = new SerializedKeyBinding("KeyThatDoesNotExist", true, false, false);
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        var resolved = service.GetAll().Single();

        resolved.CurrentBinding.ShouldBeNull();
        resolved.IsCustomized.ShouldBeTrue();
    }

    [Fact(DisplayName = "GetBinding returns the resolved binding and null for unknown ids")]
    public void GetBinding_resolves()
    {
        var service = CreateService(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        service.GetBinding("save").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS));
        service.GetBinding("missing").ShouldBeNull();
    }

    [Fact(DisplayName = "GetByCategory groups actions by their declared category")]
    public void Groups_by_category()
    {
        var service = CreateService(
            Definition("a", null, "Requests"),
            Definition("b", null, "Requests"),
            Definition("c", null, "Panels"));

        var groups = service.GetByCategory().ToList();

        groups.Count.ShouldBe(2);
        groups.Single(g => g.Key == "Requests").Count().ShouldBe(2);
    }
}
