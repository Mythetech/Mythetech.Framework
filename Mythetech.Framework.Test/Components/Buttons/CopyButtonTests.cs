using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Components.Buttons;
using Mythetech.Framework.Enums;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Buttons;

public class CopyButtonTests : BunitContext
{
    private const string JsModulePath = "./_content/Mythetech.Framework/mythetech.js";

    private readonly JSRuntimeInvocationHandler _unregisterHandler;

    public CopyButtonTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        RenderTree.Add<PopoverTestHost>();

        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("registerClipboard", _ => true);
        _unregisterHandler = module.SetupVoid("unregisterClipboard", _ => true);
    }

    [Fact(DisplayName = "CopyButton renders the copy icon initially")]
    public void CopyButton_RendersCopyIcon_Initially()
    {
        var cut = Render<MtCopyButton>(p => p.Add(x => x.Text, "hello"));

        cut.Find(".mt-copy-button").GetAttribute("data-copied").ShouldBe("False");
    }

    [Fact(DisplayName = "CopyButton registers text with JS module on first render")]
    public void CopyButton_RegistersText_OnFirstRender()
    {
        var module = JSInterop.SetupModule(JsModulePath);
        var registerInvocation = module.SetupVoid("registerClipboard", _ => true);

        var cut = Render<MtCopyButton>(p => p.Add(x => x.Text, "payload"));

        registerInvocation.Invocations.Count.ShouldBeGreaterThanOrEqualTo(1);
        registerInvocation.Invocations["registerClipboard"][0].Arguments[1].ShouldBe("payload");
    }

    [Fact(DisplayName = "CopyButton passes DotNetObjectReference to JS register call")]
    public void CopyButton_PassesDotNetRef_ToRegisterCall()
    {
        var module = JSInterop.SetupModule(JsModulePath);
        var registerInvocation = module.SetupVoid("registerClipboard", _ => true);

        var cut = Render<MtCopyButton>(p => p.Add(x => x.Text, "payload"));

        registerInvocation.Invocations["registerClipboard"][0].Arguments[2].ShouldNotBeNull();
    }

    [Fact(DisplayName = "CopyButton re-registers when Text parameter changes")]
    public void CopyButton_ReRegisters_WhenTextChanges()
    {
        var module = JSInterop.SetupModule(JsModulePath);
        var registerInvocation = module.SetupVoid("registerClipboard", _ => true);

        var cut = Render<MtCopyButton>(p => p.Add(x => x.Text, "first"));

        cut.Render(p => p.Add(x => x.Text, "second"));

        var secondArgs = registerInvocation.Invocations.Last().Arguments;
        secondArgs[1].ShouldBe("second");
    }

    [Fact(DisplayName = "CopyButton does not re-register when Text is unchanged")]
    public void CopyButton_DoesNotReRegister_WhenTextUnchanged()
    {
        var module = JSInterop.SetupModule(JsModulePath);
        var registerInvocation = module.SetupVoid("registerClipboard", _ => true);

        var cut = Render<MtCopyButton>(p => p.Add(x => x.Text, "same"));
        var countAfterFirstRender = registerInvocation.Invocations.Count;

        cut.Render(p => p.Add(x => x.Text, "same"));

        registerInvocation.Invocations.Count.ShouldBe(countAfterFirstRender);
    }

    [Fact(DisplayName = "CopyButton shows copied state on successful copy")]
    public void CopyButton_ShowsCopiedState_OnSuccess()
    {
        // Park the reset so the copied state cannot vanish between observation and assertion
        var cut = Render<MtCopyButton>(p => p
            .Add(x => x.Text, "payload")
            .Add(x => x.ResetDelay, TimeSpan.FromHours(1)));

        cut.InvokeAsync(() => cut.Instance.OnCopyResult(true));

        cut.WaitForAssertion(() =>
        {
            var element = cut.Find(".mt-copy-button");
            element.GetAttribute("data-copied").ShouldBe("True");
            element.ClassList.Contains("mt-copy-button--copied").ShouldBeTrue();
        }, TimeSpan.FromSeconds(2));
    }

    [Fact(DisplayName = "CopyButton resets to idle after the reset delay")]
    public async Task CopyButton_ResetsToIdle_AfterResetDelay()
    {
        var cut = Render<MtCopyButton>(p => p
            .Add(x => x.Text, "payload")
            .Add(x => x.ResetDelay, TimeSpan.FromMilliseconds(50)));

        await cut.InvokeAsync(() => cut.Instance.OnCopyResult(true));

        cut.Find(".mt-copy-button").GetAttribute("data-copied").ShouldBe("False");
        cut.Find(".mt-copy-button").ClassList.Contains("mt-copy-button--copied").ShouldBeFalse();
    }

    [Fact(DisplayName = "CopyButton shows failed state on copy failure")]
    public void CopyButton_ShowsFailedState_OnFailure()
    {
        var cut = Render<MtCopyButton>(p => p
            .Add(x => x.Text, "payload")
            .Add(x => x.ResetDelay, TimeSpan.FromHours(1)));

        cut.InvokeAsync(() => cut.Instance.OnCopyResult(false));

        cut.WaitForAssertion(() =>
        {
            var element = cut.Find(".mt-copy-button");
            element.ClassList.Contains("mt-copy-button--failed").ShouldBeTrue();
            element.GetAttribute("data-copied").ShouldBe("False");
        }, TimeSpan.FromSeconds(2));
    }

    [Fact(DisplayName = "CopyButton resets failed state after the reset delay")]
    public async Task CopyButton_ResetsFailedState_AfterResetDelay()
    {
        var cut = Render<MtCopyButton>(p => p
            .Add(x => x.Text, "payload")
            .Add(x => x.ResetDelay, TimeSpan.FromMilliseconds(50)));

        await cut.InvokeAsync(() => cut.Instance.OnCopyResult(false));

        cut.Find(".mt-copy-button").ClassList.Contains("mt-copy-button--failed").ShouldBeFalse();
    }

    [Fact(DisplayName = "CopyButton is disabled when Text is empty")]
    public void CopyButton_Disabled_WhenTextEmpty()
    {
        var cut = Render<MtCopyButton>(p => p.Add(x => x.Text, string.Empty));

        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "CopyButton renders data-mt-clipboard attribute")]
    public void CopyButton_Renders_DataMtClipboardAttribute()
    {
        var cut = Render<MtCopyButton>(p => p.Add(x => x.Text, "hello"));

        cut.Find("[data-mt-clipboard]").ShouldNotBeNull();
    }

    [Fact(DisplayName = "CopyButton renders with a unique element ID")]
    public void CopyButton_Renders_WithUniqueId()
    {
        var cut = Render<MtCopyButton>(p => p.Add(x => x.Text, "hello"));

        var id = cut.Find("[data-mt-clipboard]").GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();
        id.ShouldStartWith("mt-cb-");
    }

    [Fact(DisplayName = "CopyButton unregisters on dispose")]
    public async Task CopyButton_Unregisters_OnDispose()
    {
        var cut = Render<MtCopyButton>(p => p.Add(x => x.Text, "hello"));

        await DisposeComponentsAsync();

        _unregisterHandler.Invocations.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact(DisplayName = "CopyButton Text variant renders label text")]
    public void CopyButton_TextVariant_RendersLabel()
    {
        var cut = Render<MtCopyButton>(p => p
            .Add(x => x.Text, "hello")
            .Add(x => x.CopyVariant, CopyButtonVariant.Text));

        cut.Find("button").TextContent.ShouldContain("Copy");
    }

    [Fact(DisplayName = "CopyButton Text variant shows Copied label on success")]
    public void CopyButton_TextVariant_ShowsCopiedLabel_OnSuccess()
    {
        var cut = Render<MtCopyButton>(p => p
            .Add(x => x.Text, "hello")
            .Add(x => x.CopyVariant, CopyButtonVariant.Text)
            .Add(x => x.ResetDelay, TimeSpan.FromHours(1)));

        cut.InvokeAsync(() => cut.Instance.OnCopyResult(true));

        cut.WaitForAssertion(() =>
            cut.Find("button").TextContent.ShouldContain("Copied"),
            TimeSpan.FromSeconds(2));
    }

    [Fact(DisplayName = "CopyButton Text variant shows error label on failure")]
    public void CopyButton_TextVariant_ShowsErrorLabel_OnFailure()
    {
        var cut = Render<MtCopyButton>(p => p
            .Add(x => x.Text, "hello")
            .Add(x => x.CopyVariant, CopyButtonVariant.Text)
            .Add(x => x.ResetDelay, TimeSpan.FromHours(1)));

        cut.InvokeAsync(() => cut.Instance.OnCopyResult(false));

        cut.WaitForAssertion(() =>
            cut.Find("button").TextContent.ShouldContain("Copy failed"),
            TimeSpan.FromSeconds(2));
    }

    [Fact(DisplayName = "CopyButton Text variant is disabled when Text is empty")]
    public void CopyButton_TextVariant_Disabled_WhenTextEmpty()
    {
        var cut = Render<MtCopyButton>(p => p
            .Add(x => x.Text, string.Empty)
            .Add(x => x.CopyVariant, CopyButtonVariant.Text));

        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
    }
}
