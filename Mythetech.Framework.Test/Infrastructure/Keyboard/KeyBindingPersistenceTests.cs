using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MudBlazor.Utilities;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Settings.Consumers;
using Mythetech.Framework.Infrastructure.Settings.Events;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Keyboard;

/// <summary>
/// Drives the real persistence path rather than a mocked settings provider:
/// <see cref="SettingsPersister"/> produces the JSON and
/// <see cref="SettingsProvider.ApplyPersistedSettingsAsync"/> reads it back.
/// Both halves filter on the [Setting] attribute, so a missing attribute makes
/// the whole override dictionary vanish across a restart.
/// </summary>
public class KeyBindingPersistenceTests
{
    private static KeyBindingSettings NewSettings()
        => new(Options.Create(new KeyBindingSettingsOptions()));

    private static async Task<string> PersistAsync(KeyBindingSettings settings)
    {
        string? captured = null;
        var storage = Substitute.For<ISettingsStorage>();
        storage
            .When(x => x.SaveSettingsAsync(Arg.Any<string>(), Arg.Any<string>()))
            .Do(call => captured = call.ArgAt<string>(1));

        var persister = new SettingsPersister(NullLogger<SettingsPersister>.Instance, storage);
        await persister.Consume(new SettingsModelChanged(settings));

        captured.ShouldNotBeNull();
        return captured;
    }

    private static async Task<KeyBindingSettings> LoadAsync(string json)
    {
        var provider = new SettingsProvider(
            Substitute.For<IMessageBus>(),
            NullLogger<SettingsProvider>.Instance,
            new ServiceCollection().BuildServiceProvider(),
            Options.Create(new SettingsRegistrationOptions()));

        var loaded = NewSettings();
        provider.RegisterSettings(loaded);

        await provider.ApplyPersistedSettingsAsync(new Dictionary<string, string>
        {
            [loaded.SettingsId] = json,
        });

        return loaded;
    }

    [Fact(DisplayName = "The persisted payload actually carries the overrides rather than an empty object")]
    public async Task Persisted_payload_carries_overrides()
    {
        var settings = NewSettings();
        settings.Overrides["editor.save"] = SerializedKeyBinding.FromBinding(
            KeyBinding.CtrlOrCmd(JsKey.KeyS, shift: true));

        var json = await PersistAsync(settings);

        json.ShouldNotBe("{}");
        json.ShouldContain("Overrides");
        json.ShouldContain("editor.save");
    }

    [Fact(DisplayName = "An assigned override round trips through persistence")]
    public async Task Assigned_override_round_trips()
    {
        var settings = NewSettings();
        settings.Overrides["editor.save"] = SerializedKeyBinding.FromBinding(
            KeyBinding.CtrlOrCmd(JsKey.KeyS, shift: true));

        var loaded = await LoadAsync(await PersistAsync(settings));

        loaded.Overrides.ShouldContainKey("editor.save");
        loaded.Overrides["editor.save"].ShouldBe(new SerializedKeyBinding("KeyS", Ctrl: true, Shift: true, Alt: false));
        loaded.Overrides["editor.save"]!.ToBinding()
            .ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS, shift: true));
    }

    [Fact(DisplayName = "An explicit unbind round trips as a present key with a null value")]
    public async Task Explicit_unbind_round_trips_as_null_entry()
    {
        var settings = NewSettings();
        settings.Overrides["editor.format"] = null;

        var loaded = await LoadAsync(await PersistAsync(settings));

        loaded.Overrides.ShouldContainKey("editor.format");
        loaded.Overrides["editor.format"].ShouldBeNull();
    }

    [Fact(DisplayName = "Assigned, unbound and untouched actions all survive one round trip")]
    public async Task All_four_states_survive_a_round_trip()
    {
        var settings = NewSettings();
        settings.Overrides["editor.save"] = SerializedKeyBinding.FromBinding(
            KeyBinding.CtrlOrCmd(JsKey.KeyS, shift: true));
        settings.Overrides["editor.format"] = null;
        settings.Overrides["view.zen"] = SerializedKeyBinding.FromBinding(KeyBinding.Plain(JsKey.F2));

        var loaded = await LoadAsync(await PersistAsync(settings));

        loaded.Overrides.Count.ShouldBe(3);
        loaded.Overrides["editor.save"].ShouldBe(new SerializedKeyBinding("KeyS", Ctrl: true, Shift: true, Alt: false));
        loaded.Overrides["editor.format"].ShouldBeNull();
        loaded.Overrides["view.zen"].ShouldBe(new SerializedKeyBinding("F2", Ctrl: false, Shift: false, Alt: false));
        loaded.Overrides.ShouldNotContainKey("editor.never.touched");
    }

    [Fact(DisplayName = "A restored override resolves through the service to the customized binding")]
    public async Task Restored_overrides_resolve_through_the_service()
    {
        var settings = NewSettings();
        settings.Overrides["editor.save"] = SerializedKeyBinding.FromBinding(
            KeyBinding.CtrlOrCmd(JsKey.KeyS, shift: true));
        settings.Overrides["editor.format"] = null;

        var loaded = await LoadAsync(await PersistAsync(settings));

        var options = new KeyBindingRegistrationOptions();
        options.Definitions.Add(new KeyBindingDefinition(
            "editor.save", "Save", null, "Edit", KeyBinding.CtrlOrCmd(JsKey.KeyS),
            (bus, ct) => Task.CompletedTask));
        options.Definitions.Add(new KeyBindingDefinition(
            "editor.format", "Format", null, "Edit", KeyBinding.CtrlOrCmd(JsKey.KeyF),
            (bus, ct) => Task.CompletedTask));
        options.Definitions.Add(new KeyBindingDefinition(
            "editor.rename", "Rename", null, "Edit", KeyBinding.Plain(JsKey.F2),
            (bus, ct) => Task.CompletedTask));

        var service = new KeyBindingService(
            new KeyBindingRegistry(Options.Create(options)),
            loaded,
            Substitute.For<ISettingsProvider>(),
            Substitute.For<IMessageBus>(),
            NullLogger<KeyBindingService>.Instance);

        service.GetBinding("editor.save").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS, shift: true));
        service.GetBinding("editor.format").ShouldBeNull();
        service.GetBinding("editor.rename").ShouldBe(KeyBinding.Plain(JsKey.F2));
    }
}
