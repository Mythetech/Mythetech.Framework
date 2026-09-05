using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Utilities;
using Mythetech.Framework.Components.Kbd;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Kbd;

public class MtKeyBindingsEditorTests : BunitContext
{
    private readonly ISettingsProvider _settingsProvider = Substitute.For<ISettingsProvider>();
    private readonly KeyBindingSettings _settings = new(Options.Create(new KeyBindingSettingsOptions()));
    private readonly InMemoryMessageBus _bus;
    private KeyBindingService _service = default!;

    public MtKeyBindingsEditorTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IPlatformDetector>(new DefaultPlatformDetector());

        _bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());
        Services.AddSingleton<IMessageBus>(_bus);

        // Registered as a factory reading the field so Configure() can swap the
        // instance per test without adding services after the first render, which
        // bUnit's service provider forbids.
        Services.AddSingleton<IKeyBindingService>(_ => _service);

        // MtIconButton renders a MudTooltip, which needs a popover provider in the render tree.
        Render<MudPopoverProvider>();
    }

    private void Configure(params KeyBindingDefinition[] definitions)
    {
        var options = new KeyBindingRegistrationOptions();
        options.Definitions.AddRange(definitions);

        _service = new KeyBindingService(
            new KeyBindingRegistry(Options.Create(options)),
            _settings,
            _settingsProvider,
            _bus,
            NullLogger<KeyBindingService>.Instance);
    }

    private static KeyBindingDefinition Definition(string id, KeyBinding? binding, string category = "General", string? displayName = null)
        => new(id, displayName ?? id, null, category, binding, (_, _) => Task.CompletedTask);

    [Fact(DisplayName = "Renders a row per action grouped by category")]
    public void Renders_rows_by_category()
    {
        Configure(
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS), "Edit"),
            Definition("find", null, "Search"));

        var cut = Render<MtKeyBindingsEditor>();

        cut.FindAll(".mf-keybinding-row").Count.ShouldBe(2);
        cut.FindAll(".mf-keybinding-category").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Shows Not set for an action that ships unbound")]
    public void Shows_not_set_for_unbound()
    {
        Configure(Definition("duplicate", null));

        var cut = Render<MtKeyBindingsEditor>();

        cut.Find(".mf-keybinding-unset").TextContent.Trim().ShouldBe("Not set");
    }

    [Fact(DisplayName = "Recording a key assigns it to an action that shipped unbound")]
    public async Task Recording_assigns_binding()
    {
        Configure(Definition("duplicate", null));

        var cut = Render<MtKeyBindingsEditor>();
        await cut.Find(".mf-keybinding-record").ClickAsync(new MouseEventArgs());
        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "KeyD", CtrlKey = true });

        _service.GetBinding("duplicate").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyD));
    }

    [Fact(DisplayName = "Reset appears only for a customized action and removes the override")]
    public async Task Reset_appears_when_customized()
    {
        Configure(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        var cut = Render<MtKeyBindingsEditor>();
        cut.FindAll(".mf-keybinding-reset").ShouldBeEmpty();

        await _service.SetBindingAsync("save", KeyBinding.CtrlOrCmd(JsKey.KeyD));
        cut.Render();

        await cut.Find(".mf-keybinding-reset").ClickAsync(new MouseEventArgs());

        _service.GetBinding("save").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS));
    }

    [Fact(DisplayName = "Recording a key held by another action warns instead of assigning")]
    public async Task Conflict_warns_before_assigning()
    {
        Configure(
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS), displayName: "Persist Changes"),
            Definition("search", null));

        var cut = Render<MtKeyBindingsEditor>();
        var recordButtons = cut.FindAll(".mf-keybinding-record");
        await recordButtons[1].ClickAsync(new MouseEventArgs());
        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "KeyS", CtrlKey = true });

        var conflictText = cut.Find(".mf-keybinding-conflict").TextContent;
        conflictText.ShouldContain("Persist Changes");
        conflictText.ShouldNotContain("save");
        _service.GetBinding("search").ShouldBeNull();
        _service.GetBinding("save").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS));
    }

    [Fact(DisplayName = "Cancelling a conflict leaves both actions untouched")]
    public async Task Cancelling_conflict_leaves_both_actions_untouched()
    {
        Configure(
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)),
            Definition("search", null));

        var cut = Render<MtKeyBindingsEditor>();
        var recordButtons = cut.FindAll(".mf-keybinding-record");
        await recordButtons[1].ClickAsync(new MouseEventArgs());
        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "KeyS", CtrlKey = true });
        await cut.Find(".mf-keybinding-conflict-cancel").ClickAsync(new MouseEventArgs());

        _service.GetBinding("search").ShouldBeNull();
        _service.GetBinding("save").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS));
        cut.FindAll(".mf-keybinding-conflict").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Confirming a conflict reassigns and unbinds the previous holder")]
    public async Task Confirming_conflict_reassigns()
    {
        Configure(
            Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)),
            Definition("search", null));

        var cut = Render<MtKeyBindingsEditor>();
        var recordButtons = cut.FindAll(".mf-keybinding-record");
        await recordButtons[1].ClickAsync(new MouseEventArgs());
        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "KeyS", CtrlKey = true });
        await cut.Find(".mf-keybinding-reassign").ClickAsync(new MouseEventArgs());

        _service.GetBinding("search").ShouldBe(KeyBinding.CtrlOrCmd(JsKey.KeyS));
        _service.GetBinding("save").ShouldBeNull();
    }

    [Fact(DisplayName = "Clearing a bound action leaves it unbound")]
    public async Task Clear_unbinds_action()
    {
        Configure(Definition("save", KeyBinding.CtrlOrCmd(JsKey.KeyS)));

        var cut = Render<MtKeyBindingsEditor>();
        await cut.Find(".mf-keybinding-clear").ClickAsync(new MouseEventArgs());

        _service.GetBinding("save").ShouldBeNull();
    }
}
