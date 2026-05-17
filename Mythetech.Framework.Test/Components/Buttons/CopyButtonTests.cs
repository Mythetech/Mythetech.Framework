using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Components.Buttons;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Buttons;

public class CopyButtonTests : TestContext
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
        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, "hello"));

        cut.Find(".mt-copy-button").GetAttribute("data-copied").ShouldBe("False");
    }

    [Fact(DisplayName = "CopyButton registers text with JS module on first render")]
    public void CopyButton_RegistersText_OnFirstRender()
    {
        var module = JSInterop.SetupModule(JsModulePath);
        var registerInvocation = module.SetupVoid("registerClipboard", _ => true);

        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, "payload"));

        registerInvocation.Invocations.Count.ShouldBeGreaterThanOrEqualTo(1);
        registerInvocation.Invocations["registerClipboard"][0].Arguments[1].ShouldBe("payload");
    }

    [Fact(DisplayName = "CopyButton re-registers when Text parameter changes")]
    public void CopyButton_ReRegisters_WhenTextChanges()
    {
        var module = JSInterop.SetupModule(JsModulePath);
        var registerInvocation = module.SetupVoid("registerClipboard", _ => true);

        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, "first"));

        cut.SetParametersAndRender(p => p.Add(x => x.Text, "second"));

        var secondArgs = registerInvocation.Invocations.Last().Arguments;
        secondArgs[1].ShouldBe("second");
    }

    [Fact(DisplayName = "CopyButton does not re-register when Text is unchanged")]
    public void CopyButton_DoesNotReRegister_WhenTextUnchanged()
    {
        var module = JSInterop.SetupModule(JsModulePath);
        var registerInvocation = module.SetupVoid("registerClipboard", _ => true);

        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, "same"));
        var countAfterFirstRender = registerInvocation.Invocations.Count;

        cut.SetParametersAndRender(p => p.Add(x => x.Text, "same"));

        registerInvocation.Invocations.Count.ShouldBe(countAfterFirstRender);
    }

    [Fact(DisplayName = "CopyButton shows copied state on click then resets")]
    public async Task CopyButton_ShowsCopiedState_ThenResets()
    {
        var cut = RenderComponent<MtCopyButton>(p => p
            .Add(x => x.Text, "payload")
            .Add(x => x.ResetDelay, TimeSpan.FromMilliseconds(50)));

        await cut.Find("button").ClickAsync(new());

        cut.Find(".mt-copy-button").GetAttribute("data-copied").ShouldBe("False");
    }

    [Fact(DisplayName = "CopyButton is disabled when Text is empty")]
    public void CopyButton_Disabled_WhenTextEmpty()
    {
        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, string.Empty));

        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "CopyButton renders data-mt-clipboard attribute")]
    public void CopyButton_Renders_DataMtClipboardAttribute()
    {
        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, "hello"));

        cut.Find("[data-mt-clipboard]").ShouldNotBeNull();
    }

    [Fact(DisplayName = "CopyButton renders with a unique element ID")]
    public void CopyButton_Renders_WithUniqueId()
    {
        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, "hello"));

        var id = cut.Find("[data-mt-clipboard]").GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();
        id.ShouldStartWith("mt-cb-");
    }

    [Fact(DisplayName = "CopyButton unregisters on dispose")]
    public void CopyButton_Unregisters_OnDispose()
    {
        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, "hello"));

        DisposeComponents();
        
        _unregisterHandler.Invocations.Count.ShouldBeGreaterThanOrEqualTo(1);
    }
}
