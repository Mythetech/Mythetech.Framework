using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Utilities;
using Mythetech.Framework.Components.CommandPalette;
using Mythetech.Framework.Components.Kbd;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.CommandPalette;

public class CommandPaletteHostTests : TestContext
{
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly CommandPaletteService _paletteService;

    public CommandPaletteHostTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IPlatformDetector>(new DefaultPlatformDetector());

        _paletteService = new CommandPaletteService(
            Array.Empty<ICommandProvider>(),
            NullLogger<CommandPaletteService>.Instance);
        Services.AddSingleton(_paletteService);
        Services.AddSingleton(_dialogService);
    }

    [Fact(DisplayName = "Mounts two MudHotkey instances for Ctrl+K and Cmd+K")]
    public void Mounts_two_hotkeys_for_ctrl_and_cmd()
    {
        var cut = RenderComponent<CommandPaletteHost>();

        var hotkeys = cut.FindComponents<MudHotkey>();
        hotkeys.Count.ShouldBe(2);

        var keys = hotkeys.Where(h => h.Instance.Key == JsKey.KeyK).ToArray();
        keys.Length.ShouldBe(2);
        keys.SelectMany(h => h.Instance.KeyModifiers).ShouldContain(JsKeyModifier.ControlLeft);
        keys.SelectMany(h => h.Instance.KeyModifiers).ShouldContain(JsKeyModifier.MetaLeft);
    }

    [Fact(DisplayName = "Pressing the hotkey opens the CommandPaletteDialog via IDialogService")]
    public async Task Hotkey_opens_command_palette_dialog()
    {
        var cut = RenderComponent<CommandPaletteHost>();
        var ctrlHotkey = cut.FindComponents<MudHotkey>()
            .Single(h => h.Instance.Key == JsKey.KeyK && h.Instance.KeyModifiers.Contains(JsKeyModifier.ControlLeft));

        await cut.InvokeAsync(() => ctrlHotkey.Instance.MudHotkeyProviderJsCallback());

        await _dialogService.Received(1).ShowAsync(
            typeof(CommandPaletteDialog),
            Arg.Any<string>(),
            Arg.Any<DialogOptions>());
    }

    [Fact(DisplayName = "Hotkey is a no-op when palette is already open")]
    public async Task Hotkey_does_not_stack_when_palette_already_open()
    {
        _paletteService.MarkOpened();
        var cut = RenderComponent<CommandPaletteHost>();
        var ctrlHotkey = cut.FindComponents<MudHotkey>()
            .Single(h => h.Instance.Key == JsKey.KeyK && h.Instance.KeyModifiers.Contains(JsKeyModifier.ControlLeft));

        await cut.InvokeAsync(() => ctrlHotkey.Instance.MudHotkeyProviderJsCallback());

        await _dialogService.DidNotReceive().ShowAsync(
            typeof(CommandPaletteDialog),
            Arg.Any<string>(),
            Arg.Any<DialogOptions>());
    }

    [Fact(DisplayName = "Both Ctrl+K and Cmd+K hotkeys open the palette")]
    public async Task Both_hotkeys_open_palette()
    {
        var cut = RenderComponent<CommandPaletteHost>();
        var hotkeys = cut.FindComponents<MudHotkey>()
            .Where(h => h.Instance.Key == JsKey.KeyK).ToArray();

        await cut.InvokeAsync(() => hotkeys[0].Instance.MudHotkeyProviderJsCallback());
        _paletteService.MarkClosed();
        await cut.InvokeAsync(() => hotkeys[1].Instance.MudHotkeyProviderJsCallback());

        await _dialogService.Received(2).ShowAsync(
            typeof(CommandPaletteDialog),
            Arg.Any<string>(),
            Arg.Any<DialogOptions>());
    }
}
