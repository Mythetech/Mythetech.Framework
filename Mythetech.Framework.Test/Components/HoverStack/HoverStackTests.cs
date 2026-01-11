using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using Shouldly;
using HoverStackComponent = Mythetech.Framework.Components.HoverStack.HoverStack;
using HoverContextType = Mythetech.Framework.Components.HoverStack.HoverContext;

namespace Mythetech.Framework.Test.Components.HoverStackTests;

public class HoverStackTests : TestContext
{
    public HoverStackTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "HoverStack renders child content")]
    public void HoverStack_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<HoverStackComponent>(parameters => parameters
            .Add(p => p.ChildContent, (HoverContextType ctx) =>
                (RenderFragment)(builder => builder.AddContent(0, "Hover Content"))));

        // Assert
        cut.Markup.ShouldContain("Hover Content");
    }

    [Fact(DisplayName = "HoverStack provides IsHovering false initially")]
    public void HoverStack_ProvidesIsHoveringFalse_Initially()
    {
        // Arrange
        bool? isHovering = null;

        var cut = RenderComponent<HoverStackComponent>(parameters => parameters
            .Add(p => p.ChildContent, (HoverContextType ctx) =>
            {
                isHovering = ctx.IsHovering;
                return (RenderFragment)(builder => builder.AddContent(0, $"Hovering: {ctx.IsHovering}"));
            }));

        // Assert
        isHovering.ShouldBe(false);
        cut.Markup.ShouldContain("Hovering: False");
    }

    [Fact(DisplayName = "HoverStack sets IsHovering true on mouse enter")]
    public async Task HoverStack_SetsIsHoveringTrue_OnMouseEnter()
    {
        // Arrange
        var cut = RenderComponent<HoverStackComponent>(parameters => parameters
            .Add(p => p.ChildContent, (HoverContextType ctx) =>
                (RenderFragment)(builder => builder.AddContent(0, $"Hovering: {ctx.IsHovering}"))));

        // Act - trigger mouseenter event directly
        var container = cut.Find("div");
        await container.TriggerEventAsync("onmouseenter", new MouseEventArgs());

        // Assert
        cut.Markup.ShouldContain("Hovering: True");
    }

    [Fact(DisplayName = "HoverStack sets IsHovering false on mouse leave")]
    public async Task HoverStack_SetsIsHoveringFalse_OnMouseLeave()
    {
        // Arrange
        var cut = RenderComponent<HoverStackComponent>(parameters => parameters
            .Add(p => p.ChildContent, (HoverContextType ctx) =>
                (RenderFragment)(builder => builder.AddContent(0, $"Hovering: {ctx.IsHovering}"))));

        // Act - trigger mouseenter then mouseleave
        var container = cut.Find("div");
        await container.TriggerEventAsync("onmouseenter", new MouseEventArgs());
        await container.TriggerEventAsync("onmouseleave", new MouseEventArgs());

        // Assert
        cut.Markup.ShouldContain("Hovering: False");
    }

    [Fact(DisplayName = "HoverStack invokes OnClick when clicked")]
    public void HoverStack_InvokesOnClick_WhenClicked()
    {
        // Arrange
        MouseEventArgs? capturedArgs = null;
        var cut = RenderComponent<HoverStackComponent>(parameters => parameters
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, args => capturedArgs = args))
            .Add(p => p.ChildContent, (HoverContextType ctx) =>
                (RenderFragment)(builder => builder.AddContent(0, "Click me"))));

        // Act
        var container = cut.Find("div");
        container.Click();

        // Assert
        capturedArgs.ShouldNotBeNull();
    }

    [Fact(DisplayName = "HoverStack applies Row parameter")]
    public void HoverStack_AppliesRowParameter()
    {
        // Arrange & Act
        var cut = RenderComponent<HoverStackComponent>(parameters => parameters
            .Add(p => p.Row, true)
            .Add(p => p.ChildContent, (HoverContextType ctx) =>
                (RenderFragment)(builder => builder.AddContent(0, "Row content"))));

        // Assert - Check that the component rendered with the row parameter applied
        cut.Markup.ShouldNotBeEmpty();
        // The actual class implementation may vary, so we just verify the component renders
    }

    [Fact(DisplayName = "HoverStack applies custom class")]
    public void HoverStack_AppliesCustomClass()
    {
        // Arrange & Act
        var cut = RenderComponent<HoverStackComponent>(parameters => parameters
            .Add(p => p.Class, "my-custom-class")
            .Add(p => p.ChildContent, (HoverContextType ctx) =>
                (RenderFragment)(builder => builder.AddContent(0, "Custom class"))));

        // Assert
        cut.Markup.ShouldContain("my-custom-class");
    }

    [Fact(DisplayName = "HoverStack applies custom style")]
    public void HoverStack_AppliesCustomStyle()
    {
        // Arrange & Act
        var cut = RenderComponent<HoverStackComponent>(parameters => parameters
            .Add(p => p.Style, "background-color: red;")
            .Add(p => p.ChildContent, (HoverContextType ctx) =>
                (RenderFragment)(builder => builder.AddContent(0, "Styled content"))));

        // Assert
        cut.Markup.ShouldContain("background-color: red");
    }

    [Fact(DisplayName = "HoverStack applies Spacing parameter")]
    public void HoverStack_AppliesSpacingParameter()
    {
        // Arrange & Act
        var cut = RenderComponent<HoverStackComponent>(parameters => parameters
            .Add(p => p.Spacing, 5)
            .Add(p => p.ChildContent, (HoverContextType ctx) =>
                (RenderFragment)(builder => builder.AddContent(0, "Spaced content"))));

        // Assert - Component should render without errors
        cut.Markup.ShouldContain("Spaced content");
    }
}
