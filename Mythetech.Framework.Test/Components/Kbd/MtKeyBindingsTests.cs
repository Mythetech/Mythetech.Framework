using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Utilities;
using Mythetech.Framework.Components.Kbd;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Events;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Kbd;

public class MtKeyBindingsTests : BunitContext
{
    private readonly IKeyBindingService _service = Substitute.For<IKeyBindingService>();
    private readonly KeyBindingSettings _settings = new(Options.Create(new KeyBindingSettingsOptions()));
    private readonly InMemoryMessageBus _bus;

    public MtKeyBindingsTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        Services.AddSingleton<IMessageBus>(_bus);
        Services.AddSingleton(_service);
        Services.AddSingleton<IPlatformDetector>(new DefaultPlatformDetector());
    }

    private static ResolvedKeyBinding Resolved(string id, KeyBinding? binding)
        => new(new KeyBindingDefinition(id, id, null, "General", binding, (_, _) => Task.CompletedTask),
            binding,
            IsCustomized: false);

    [Fact(DisplayName = "Renders one MudHotkey per bound action")]
    public void Renders_hotkey_per_bound_action()
    {
        _service.GetAll().Returns(new[]
        {
            Resolved("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)),
            Resolved("find", KeyBinding.CtrlOrCmd(JsKey.KeyF, shift: true)),
        });

        var cut = Render<MtKeyBindings>();

        cut.FindComponents<MudHotkey>().Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Renders no MudHotkey for an unbound action")]
    public void Renders_nothing_for_unbound_action()
    {
        _service.GetAll().Returns(new[] { Resolved("duplicate", null) });

        var cut = Render<MtKeyBindings>();

        cut.FindComponents<MudHotkey>().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Maps the accelerator to ControlLeft when not on macOS")]
    public void Maps_accelerator_to_control_off_mac()
    {
        _service.GetAll().Returns(new[] { Resolved("save", KeyBinding.CtrlOrCmd(JsKey.KeyS, shift: true)) });

        var cut = Render<MtKeyBindings>();
        var hotkey = cut.FindComponent<MudHotkey>().Instance;

        hotkey.Key.ShouldBe(JsKey.KeyS);
        hotkey.KeyModifiers.ShouldContain(JsKeyModifier.ControlLeft);
        hotkey.KeyModifiers.ShouldContain(JsKeyModifier.ShiftLeft);
        hotkey.KeyModifiers.ShouldNotContain(JsKeyModifier.MetaLeft);
    }

    [Fact(DisplayName = "An accelerator binding fires on either Cmd or Ctrl on macOS")]
    public void Accelerator_registers_both_variants_on_mac()
    {
        Services.AddSingleton<IPlatformDetector>(new MacPlatformDetector());
        _service.GetAll().Returns(new[] { Resolved("save", KeyBinding.CtrlOrCmd(JsKey.KeyS, shift: true)) });

        var cut = Render<MtKeyBindings>();
        var hotkeys = cut.FindComponents<MudHotkey>().Select(h => h.Instance).ToArray();

        hotkeys.Length.ShouldBe(2);
        hotkeys.ShouldAllBe(h => h.Key == JsKey.KeyS);
        hotkeys.ShouldContain(h => h.KeyModifiers.Contains(JsKeyModifier.MetaLeft));
        hotkeys.ShouldContain(h => h.KeyModifiers.Contains(JsKeyModifier.ControlLeft));

        foreach (var hotkey in hotkeys)
        {
            hotkey.KeyModifiers.ShouldContain(JsKeyModifier.ShiftLeft);
        }

        var meta = hotkeys.Single(h => h.KeyModifiers.Contains(JsKeyModifier.MetaLeft));
        meta.KeyModifiers.ShouldNotContain(JsKeyModifier.ControlLeft);

        var ctrl = hotkeys.Single(h => h.KeyModifiers.Contains(JsKeyModifier.ControlLeft));
        ctrl.KeyModifiers.ShouldNotContain(JsKeyModifier.MetaLeft);
    }

    [Fact(DisplayName = "Either accelerator variant invokes the same action on macOS")]
    public async Task Either_variant_invokes_the_same_action_on_mac()
    {
        Services.AddSingleton<IPlatformDetector>(new MacPlatformDetector());
        _service.GetAll().Returns(new[] { Resolved("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)) });

        var cut = Render<MtKeyBindings>();
        var hotkeys = cut.FindComponents<MudHotkey>().ToArray();

        var ctrlVariant = hotkeys.Single(h => h.Instance.KeyModifiers.Contains(JsKeyModifier.ControlLeft));
        await cut.InvokeAsync(() => ctrlVariant.Instance.MudHotkeyProviderJsCallback());

        await _service.Received(1).InvokeAsync("save", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A binding with no accelerator registers a single hotkey on macOS")]
    public void Non_accelerator_binding_registers_once_on_mac()
    {
        Services.AddSingleton<IPlatformDetector>(new MacPlatformDetector());
        _service.GetAll().Returns(new[] { Resolved("help", KeyBinding.Plain(JsKey.F1)) });

        var cut = Render<MtKeyBindings>();

        cut.FindComponents<MudHotkey>().Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Pressing the hotkey invokes the action")]
    public async Task Pressing_hotkey_invokes_action()
    {
        _service.GetAll().Returns(new[] { Resolved("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)) });

        var cut = Render<MtKeyBindings>();
        var hotkey = cut.FindComponent<MudHotkey>().Instance;

        await cut.InvokeAsync(() => hotkey.MudHotkeyProviderJsCallback());

        await _service.Received(1).InvokeAsync("save", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A key binding settings change re-registers the hotkeys")]
    public async Task Settings_change_rerenders()
    {
        _service.GetAll().Returns(new[] { Resolved("duplicate", null) });

        var cut = Render<MtKeyBindings>();
        cut.FindComponents<MudHotkey>().ShouldBeEmpty();

        _service.GetAll().Returns(new[] { Resolved("duplicate", KeyBinding.CtrlOrCmd(JsKey.KeyD)) });
        await _bus.PublishAsync(new SettingsModelChanged(_settings));

        cut.WaitForAssertion(() => cut.FindComponents<MudHotkey>().Count.ShouldBe(1));
    }

    private sealed class MacPlatformDetector : IPlatformDetector
    {
        public bool IsMacOS => true;
    }
}
