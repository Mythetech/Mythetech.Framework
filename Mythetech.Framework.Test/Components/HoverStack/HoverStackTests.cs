using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using Shouldly;
using HoverStackComponent = Mythetech.Framework.Components.HoverStack.HoverStack;

namespace Mythetech.Framework.Test.Components.HoverStackTests;

public class HoverStackTests : BunitContext
{
    public HoverStackTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment Text(string text) => builder => builder.AddContent(0, text);

    [Fact(DisplayName = "HoverStack renders child content")]
    public void HoverStack_RendersChildContent()
    {
        var cut = Render<HoverStackComponent>(parameters => parameters
            .Add(p => p.ChildContent, Text("Hover Content")));

        cut.Find(".hover-stack-content").TextContent.ShouldContain("Hover Content");
    }

    [Fact(DisplayName = "HoverStack renders actions as a direct child of the hover row")]
    public void HoverStack_RendersActions_InHoverActionsContainer()
    {
        var cut = Render<HoverStackComponent>(parameters => parameters
            .Add(p => p.ChildContent, Text("Item"))
            .Add(p => p.Actions, Text("Delete")));

        var actions = cut.Find(".mt-hover-row > .mt-hover-actions");
        actions.TextContent.ShouldContain("Delete");
    }

    [Fact(DisplayName = "HoverStack omits the actions container when there are no actions")]
    public void HoverStack_OmitsActionsContainer_WithoutActions()
    {
        var cut = Render<HoverStackComponent>(parameters => parameters
            .Add(p => p.ChildContent, Text("Item")));

        cut.FindAll(".mt-hover-actions").ShouldBeEmpty();
    }

    [Fact(DisplayName = "HoverStack does not wrap by default")]
    public void HoverStack_DoesNotWrap_ByDefault()
    {
        var cut = Render<HoverStackComponent>(parameters => parameters
            .Add(p => p.Row, true)
            .Add(p => p.ChildContent, Text("Item")));

        cut.Find(".hover-stack-content").ClassList.ShouldContain("flex-nowrap");
    }

    [Fact(DisplayName = "HoverStack wraps when Wrap is set")]
    public void HoverStack_Wraps_WhenWrapIsSet()
    {
        var cut = Render<HoverStackComponent>(parameters => parameters
            .Add(p => p.Row, true)
            .Add(p => p.Wrap, Wrap.Wrap)
            .Add(p => p.ChildContent, Text("Item")));

        cut.Find(".hover-stack-content").ClassList.ShouldContain("flex-wrap");
    }

    [Fact(DisplayName = "HoverStack invokes OnClick when clicked")]
    public async Task HoverStack_InvokesOnClick_WhenClicked()
    {
        MouseEventArgs? capturedArgs = null;
        var cut = Render<HoverStackComponent>(parameters => parameters
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, args => capturedArgs = args))
            .Add(p => p.ChildContent, Text("Click me")));

        await cut.Find(".mt-hover-row").ClickAsync(new MouseEventArgs());

        capturedArgs.ShouldNotBeNull();
    }

    [Fact(DisplayName = "HoverStack applies Class and Style to the row so they cover the actions too")]
    public void HoverStack_AppliesClassAndStyle_ToRow()
    {
        var cut = Render<HoverStackComponent>(parameters => parameters
            .Add(p => p.Class, "my-custom-class")
            .Add(p => p.Style, "background-color: red;")
            .Add(p => p.ChildContent, Text("Styled content")));

        var row = cut.Find(".mt-hover-row");
        row.ClassList.ShouldContain("my-custom-class");
        row.GetAttribute("style").ShouldContain("background-color: red");
    }

    [Fact(DisplayName = "HoverStack applies Row and Spacing to its content stack")]
    public void HoverStack_AppliesRowAndSpacing_ToContent()
    {
        var cut = Render<HoverStackComponent>(parameters => parameters
            .Add(p => p.Row, true)
            .Add(p => p.Spacing, 5)
            .Add(p => p.ChildContent, Text("Row content")));

        var content = cut.Find(".hover-stack-content");
        content.ClassList.ShouldContain("flex-row");
        content.ClassList.ShouldContain("gap-5");
    }
}
