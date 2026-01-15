using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Buttons;
using Mythetech.Framework.Enums;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Buttons;

public class ButtonTests : TestContext
{
    public ButtonTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "Button renders with text")]
    public void Button_RendersWithText()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Click Me"));

        // Assert
        cut.Markup.ShouldContain("Click Me");
    }

    [Fact(DisplayName = "Button invokes OnClick when clicked")]
    public void Button_InvokesOnClick_WhenClicked()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Click Me")
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true)));

        // Act
        cut.Find("button").Click();

        // Assert
        clicked.ShouldBeTrue();
    }

    [Fact(DisplayName = "Button applies filled variant by default")]
    public void Button_AppliesFilledVariant_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Default"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("mud-button-filled");
    }

    [Fact(DisplayName = "Button applies outlined variant")]
    public void Button_AppliesOutlinedVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Outlined")
            .Add(p => p.Variant, Variant.Outlined));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("mud-button-outlined");
    }

    [Fact(DisplayName = "Button applies text variant")]
    public void Button_AppliesTextVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Text")
            .Add(p => p.Variant, Variant.Text));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("mud-button-text");
    }

    [Fact(DisplayName = "Button applies primary color by default")]
    public void Button_AppliesPrimaryColor_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Primary"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("mud-button-filled-primary");
    }

    [Fact(DisplayName = "Button applies custom color")]
    public void Button_AppliesCustomColor()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Success")
            .Add(p => p.Color, Color.Success));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("mud-button-filled-success");
    }

    [Fact(DisplayName = "Button applies roundedness class")]
    public void Button_AppliesRoundednessClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Rounded")
            .Add(p => p.Roundedness, Roundedness.Pill));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("rounded-pill");
    }

    [Fact(DisplayName = "Button applies custom class")]
    public void Button_AppliesCustomClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Custom")
            .Add(p => p.Class, "my-custom-class"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("my-custom-class");
    }

    [Fact(DisplayName = "Button applies custom style")]
    public void Button_AppliesCustomStyle()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Text, "Styled")
            .Add(p => p.Style, "background-color: red;"));

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("style")!.ShouldContain("background-color: red");
    }
}
