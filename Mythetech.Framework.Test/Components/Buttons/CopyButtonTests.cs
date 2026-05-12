using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Components.Buttons;
using Mythetech.Framework.Infrastructure;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Buttons;

public class CopyButtonTests : TestContext
{
    private readonly ICopyToClipboard _clipboard;

    public CopyButtonTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        RenderTree.Add<PopoverTestHost>();

        _clipboard = Substitute.For<ICopyToClipboard>();
        _clipboard.CopyToClipboardAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        Services.AddSingleton(_clipboard);
    }

    [Fact(DisplayName = "CopyButton renders the copy icon initially")]
    public void CopyButton_RendersCopyIcon_Initially()
    {
        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, "hello"));

        cut.Find(".mt-copy-button").GetAttribute("data-copied").ShouldBe("False");
    }

    [Fact(DisplayName = "CopyButton invokes clipboard service with the Text parameter on click")]
    public async Task CopyButton_InvokesClipboard_OnClick()
    {
        var cut = RenderComponent<MtCopyButton>(p => p
            .Add(x => x.Text, "payload")
            .Add(x => x.ResetDelay, TimeSpan.FromMilliseconds(10)));

        await cut.Find("button").ClickAsync(new());

        await _clipboard.Received(1).CopyToClipboardAsync("payload");
    }

    [Fact(DisplayName = "CopyButton resets to copy state after ResetDelay elapses")]
    public async Task CopyButton_ResetsToCopyState_AfterDelay()
    {
        var cut = RenderComponent<MtCopyButton>(p => p
            .Add(x => x.Text, "payload")
            .Add(x => x.ResetDelay, TimeSpan.FromMilliseconds(50)));

        await cut.Find("button").ClickAsync(new());

        cut.WaitForAssertion(
            () => cut.Find(".mt-copy-button").GetAttribute("data-copied").ShouldBe("False"),
            timeout: TimeSpan.FromSeconds(2));
    }

    [Fact(DisplayName = "CopyButton is disabled when Text is empty")]
    public void CopyButton_Disabled_WhenTextEmpty()
    {
        var cut = RenderComponent<MtCopyButton>(p => p.Add(x => x.Text, string.Empty));

        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "CopyButton does not invoke clipboard when Text is empty")]
    public async Task CopyButton_DoesNotInvokeClipboard_WhenTextEmpty()
    {
        var cut = RenderComponent<MtCopyButton>(p => p
            .Add(x => x.Text, string.Empty)
            .Add(x => x.Disabled, false));

        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
        await _clipboard.DidNotReceive().CopyToClipboardAsync(Arg.Any<string>());
    }
}
