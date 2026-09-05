using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudBlazor.Utilities;
using Mythetech.Framework.Components.Kbd;
using Mythetech.Framework.Infrastructure.Keyboard;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Kbd;

public class MtKeyBindingRecorderTests : BunitContext
{
    public MtKeyBindingRecorderTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "Records a key pressed with the accelerator")]
    public async Task Records_accelerator_combination()
    {
        KeyBinding? recorded = null;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, b => recorded = b)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs
        {
            Code = "KeyB",
            CtrlKey = true,
            ShiftKey = true,
        });

        recorded.ShouldBe(new KeyBinding(JsKey.KeyB, Ctrl: true, Shift: true));
    }

    [Fact(DisplayName = "Treats Meta as the accelerator so macOS records Cmd combinations")]
    public async Task Records_meta_as_accelerator()
    {
        KeyBinding? recorded = null;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, b => recorded = b)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "KeyB", MetaKey = true });

        recorded.ShouldBe(new KeyBinding(JsKey.KeyB, Ctrl: true));
    }

    [Fact(DisplayName = "Ignores a bare modifier press and keeps waiting")]
    public async Task Ignores_bare_modifier()
    {
        var recordedCount = 0;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, _ => recordedCount++)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "ShiftLeft", ShiftKey = true });

        recordedCount.ShouldBe(0);
    }

    [Fact(DisplayName = "Escape cancels instead of recording a binding")]
    public async Task Escape_cancels()
    {
        var cancelled = false;
        var recordedCount = 0;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.Cancelled, EventCallback.Factory.Create(this, () => cancelled = true))
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, _ => recordedCount++)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "Escape" });

        cancelled.ShouldBeTrue();
        recordedCount.ShouldBe(0);
    }

    [Fact(DisplayName = "Rejects a modifier-less letter because it would fire while typing")]
    public async Task Rejects_bare_letter()
    {
        var recordedCount = 0;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, _ => recordedCount++)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "KeyR" });

        recordedCount.ShouldBe(0);
        cut.Markup.ShouldContain("Add Ctrl, Shift or Alt");
    }

    [Fact(DisplayName = "Accepts a modifier-less function key")]
    public async Task Accepts_bare_function_key()
    {
        KeyBinding? recorded = null;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, b => recorded = b)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "F2" });

        recorded.ShouldBe(KeyBinding.Plain(JsKey.F2));
    }

    [Fact(DisplayName = "Rejects a modifier-less Delete because binding it would disable the key app wide")]
    public async Task Rejects_bare_delete()
    {
        var recordedCount = 0;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, _ => recordedCount++)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "Delete" });

        recordedCount.ShouldBe(0);
        cut.Markup.ShouldContain("Add Ctrl, Shift or Alt");
    }

    [Fact(DisplayName = "Rejects a modifier-less arrow key because binding it would break every list and dropdown")]
    public async Task Rejects_bare_arrow_key()
    {
        var recordedCount = 0;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, _ => recordedCount++)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "ArrowDown" });

        recordedCount.ShouldBe(0);
        cut.Markup.ShouldContain("Add Ctrl, Shift or Alt");
    }

    [Fact(DisplayName = "Still accepts a navigation key when it carries a modifier")]
    public async Task Accepts_modified_navigation_key()
    {
        KeyBinding? recorded = null;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, b => recorded = b)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs
        {
            Code = "ArrowDown",
            AltKey = true,
        });

        recorded.ShouldBe(new KeyBinding(JsKey.ArrowDown, Alt: true));
    }

    [Fact(DisplayName = "Reports an unsupported key rather than recording it")]
    public async Task Reports_unsupported_key()
    {
        var recordedCount = 0;
        var cut = Render<MtKeyBindingRecorder>(parameters => parameters
            .Add(p => p.BindingRecorded, EventCallback.Factory.Create<KeyBinding>(this, _ => recordedCount++)));

        await cut.Find(".mf-keybinding-recorder").KeyDownAsync(new KeyboardEventArgs { Code = "Fn", CtrlKey = true });

        recordedCount.ShouldBe(0);
        cut.Markup.ShouldContain("Unsupported key");
    }
}
