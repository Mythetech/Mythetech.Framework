using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Badge;
using Mythetech.Framework.Enums;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Badge;

public class BadgeTests : TestContext
{
    public BadgeTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "Badge renders with text content")]
    public void Badge_RendersWithTextContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Test Badge"));

        // Assert
        cut.Markup.ShouldContain("Test Badge");
    }

    [Fact(DisplayName = "Badge renders with child content")]
    public void Badge_RendersWithChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .AddChildContent("Child Content"));

        // Assert
        cut.Markup.ShouldContain("Child Content");
    }

    [Fact(DisplayName = "Badge child content takes precedence over text")]
    public void Badge_ChildContentTakesPrecedenceOverText()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Text Content")
            .AddChildContent("Child Content"));

        // Assert
        cut.Markup.ShouldContain("Child Content");
        cut.Markup.ShouldNotContain("Text Content");
    }

    [Fact(DisplayName = "Badge applies correct color class")]
    public void Badge_AppliesCorrectColorClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.Color, Color.Success));

        // Assert
        var badge = cut.Find(".mythetech-badge");
        badge.ClassList.ShouldContain("mythetech-badge--success");
    }

    [Fact(DisplayName = "Badge applies correct size class")]
    public void Badge_AppliesCorrectSizeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.Size, Size.Large));

        // Assert
        var badge = cut.Find(".mythetech-badge");
        badge.ClassList.ShouldContain("mythetech-badge--large");
    }

    [Fact(DisplayName = "Badge applies correct variant class")]
    public void Badge_AppliesCorrectVariantClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.Variant, Variant.Outlined));

        // Assert
        var badge = cut.Find(".mythetech-badge");
        badge.ClassList.ShouldContain("mythetech-badge--outlined");
    }

    [Fact(DisplayName = "Badge is clickable when OnClick is provided")]
    public void Badge_IsClickable_WhenOnClickProvided()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Click Me")
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        var badge = cut.Find(".mythetech-badge");
        badge.Click();

        // Assert
        clicked.ShouldBeTrue();
        badge.ClassList.ShouldContain("mythetech-badge--clickable");
    }

    [Fact(DisplayName = "Badge has button role when clickable")]
    public void Badge_HasButtonRole_WhenClickable()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Click Me")
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => { })));

        // Assert
        var badge = cut.Find(".mythetech-badge");
        badge.GetAttribute("role").ShouldBe("button");
    }

    [Fact(DisplayName = "Badge does not have role when not clickable")]
    public void Badge_DoesNotHaveRole_WhenNotClickable()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Not Clickable"));

        // Assert
        var badge = cut.Find(".mythetech-badge");
        badge.GetAttribute("role").ShouldBeNull();
    }

    [Fact(DisplayName = "Disabled badge has disabled class")]
    public void DisabledBadge_HasDisabledClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Disabled")
            .Add(p => p.Disabled, true));

        // Assert
        var badge = cut.Find(".mythetech-badge");
        badge.ClassList.ShouldContain("mythetech-badge--disabled");
    }

    [Fact(DisplayName = "Badge renders icon when provided")]
    public void Badge_RendersIcon_WhenProvided()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "With Icon")
            .Add(p => p.Icon, Icons.Material.Filled.Check));

        // Assert
        cut.Markup.ShouldContain("mythetech-badge-icon");
    }

    [Fact(DisplayName = "Badge responds to Enter key when clickable")]
    public async Task Badge_RespondsToEnterKey_WhenClickable()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Press Enter")
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        var badge = cut.Find(".mythetech-badge");
        await badge.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        clicked.ShouldBeTrue();
    }

    [Fact(DisplayName = "Badge responds to Space key when clickable")]
    public async Task Badge_RespondsToSpaceKey_WhenClickable()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Press Space")
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        var badge = cut.Find(".mythetech-badge");
        await badge.KeyDownAsync(new KeyboardEventArgs { Key = " " });

        // Assert
        clicked.ShouldBeTrue();
    }

    [Fact(DisplayName = "Disabled badge does not respond to keyboard")]
    public async Task DisabledBadge_DoesNotRespondToKeyboard()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Disabled")
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        var badge = cut.Find(".mythetech-badge");
        await badge.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        clicked.ShouldBeFalse();
    }

    [Fact(DisplayName = "Badge has tabindex when clickable and not disabled")]
    public void Badge_HasTabindex_WhenClickableAndNotDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<Mythetech.Framework.Components.Badge.Badge>(parameters => parameters
            .Add(p => p.Text, "Focusable")
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => { })));

        // Assert
        var badge = cut.Find(".mythetech-badge");
        badge.GetAttribute("tabindex").ShouldBe("0");
    }
}
