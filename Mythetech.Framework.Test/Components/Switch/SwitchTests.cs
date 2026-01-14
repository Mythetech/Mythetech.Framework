using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Switch;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Switch;

public class SwitchTests : TestContext
{
    public SwitchTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "Switch renders with initial false value")]
    public void Switch_RendersWithInitialFalseValue()
    {
        // Arrange & Act
        var cut = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, false)
            .Add(p => p.Color, Color.Primary));

        // Assert
        var switchBase = cut.Find(".mud-switch-base-m3");
        switchBase.ClassList.ShouldNotContain("mud-checked");

        var input = cut.Find("input[type='checkbox']");
        input.GetAttribute("checked").ShouldBeNull();
    }

    [Fact(DisplayName = "Switch input element has onchange handler")]
    public void Switch_InputElement_HasOnChangeHandler()
    {
        // Arrange & Act
        var cut = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, false)
            .Add(p => p.Color, Color.Primary));

        // Get the input element and check for blazor event attributes
        var input = cut.Find("input[type='checkbox']");

        // The input should have blazor's event handler attribute
        // Blazor renders event handlers with blazor:onchange or similar
        var markup = cut.Markup;

        // Check if the input has any event-related attributes
        // Blazor event handlers show up in the rendered markup
        Assert.True(markup.Contains("blazor"), $"Input should have Blazor event handlers. Markup:\n{markup}");
    }

    [Fact(DisplayName = "Switch renders with initial true value")]
    public void Switch_RendersWithInitialTrueValue()
    {
        // Arrange & Act
        var cut = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, true)
            .Add(p => p.Color, Color.Primary));

        // Assert
        var switchBase = cut.Find(".mud-switch-base-m3");
        switchBase.ClassList.ShouldContain("mud-checked");

        var input = cut.Find("input[type='checkbox']");
        input.GetAttribute("checked").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Switch toggles from false to true when clicked")]
    public void Switch_TogglesFromFalseToTrue_WhenClicked()
    {
        // Arrange
        var value = false;
        var valueChangedCount = 0;

        var cut = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, newValue =>
            {
                value = newValue;
                valueChangedCount++;
            }))
            .Add(p => p.Color, Color.Primary));

        // Act - click the checkbox input
        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // Assert
        value.ShouldBeTrue("Value should be true after clicking");
        valueChangedCount.ShouldBe(1, "ValueChanged should have been called once");
    }

    [Fact(DisplayName = "Switch toggles from true to false when clicked")]
    public void Switch_TogglesFromTrueToFalse_WhenClicked()
    {
        // Arrange
        var value = true;
        var valueChangedCount = 0;

        var cut = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, newValue =>
            {
                value = newValue;
                valueChangedCount++;
            }))
            .Add(p => p.Color, Color.Primary));

        // Act - click the checkbox input
        var input = cut.Find("input[type='checkbox']");
        input.Change(false);

        // Assert
        value.ShouldBeFalse("Value should be false after clicking");
        valueChangedCount.ShouldBe(1, "ValueChanged should have been called once");
    }

    [Fact(DisplayName = "Switch updates visual state when value changes")]
    public void Switch_UpdatesVisualState_WhenValueChanges()
    {
        // Arrange
        var value = false;

        var cut = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, newValue => value = newValue))
            .Add(p => p.Color, Color.Primary));

        // Verify initial state
        var switchBase = cut.Find(".mud-switch-base-m3");
        switchBase.ClassList.ShouldNotContain("mud-checked");

        // Act - change the value
        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // Re-render with new value
        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Value, value));

        // Assert - visual state should update
        switchBase = cut.Find(".mud-switch-base-m3");
        switchBase.ClassList.ShouldContain("mud-checked");
    }

    [Fact(DisplayName = "Switch does not toggle when disabled")]
    public void Switch_DoesNotToggle_WhenDisabled()
    {
        // Arrange
        var value = false;
        var valueChangedCount = 0;

        var cut = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.Disabled, true)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, newValue =>
            {
                value = newValue;
                valueChangedCount++;
            }))
            .Add(p => p.Color, Color.Primary));

        // Act - try to click the checkbox input
        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // Assert - value should not change when disabled
        value.ShouldBeFalse("Value should remain false when switch is disabled");
        valueChangedCount.ShouldBe(0, "ValueChanged should not be called when disabled");
    }

    [Fact(DisplayName = "Switch can be toggled multiple times")]
    public void Switch_CanBeToggledMultipleTimes()
    {
        // Arrange
        var value = false;
        var valueChangedCount = 0;

        var cut = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, newValue =>
            {
                value = newValue;
                valueChangedCount++;
            }))
            .Add(p => p.Color, Color.Primary));

        var input = cut.Find("input[type='checkbox']");

        // Act - toggle on
        input.Change(true);
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Value, value));

        // Act - toggle off
        input.Change(false);
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Value, value));

        // Act - toggle on again
        input.Change(true);
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Value, value));

        // Assert
        value.ShouldBeTrue("Final value should be true");
        valueChangedCount.ShouldBe(3, "ValueChanged should have been called three times");
    }

    [Fact(DisplayName = "Switch with two-way binding updates correctly")]
    public void Switch_WithTwoWayBinding_UpdatesCorrectly()
    {
        // Arrange - simulating a parent component scenario
        var parentValue = false;
        IRenderedComponent<Switch<bool>>? cut = null;

        cut = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, parentValue)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, newValue =>
            {
                parentValue = newValue;
                // Simulate parent re-rendering with new value
                cut?.SetParametersAndRender(p => p.Add(x => x.Value, newValue));
            }))
            .Add(p => p.Color, Color.Primary));

        // Act
        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // Assert - both the parent value and the visual state should be updated
        parentValue.ShouldBeTrue("Parent value should be updated");

        var switchBase = cut.Find(".mud-switch-base-m3");
        switchBase.ClassList.ShouldContain("mud-checked", "Visual state should reflect the new value");
    }

    [Fact(DisplayName = "Custom Switch has same input structure as MudSwitch")]
    public void Custom_Switch_Has_Same_Input_Structure_As_MudSwitch()
    {
        // Render MudSwitch
        var mudSwitch = RenderComponent<MudSwitch<bool>>(parameters => parameters
            .Add(p => p.Value, false)
            .Add(p => p.Color, Color.Primary));

        // Render custom Switch
        var customSwitch = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, false)
            .Add(p => p.Color, Color.Primary));

        var mudInput = mudSwitch.Find("input[type='checkbox']");
        var customInput = customSwitch.Find("input[type='checkbox']");

        // Both should have blazor:onchange attribute
        mudInput.OuterHtml.ShouldContain("blazor:onchange");
        customInput.OuterHtml.ShouldContain("blazor:onchange");
    }

    [Fact(DisplayName = "Custom Switch should have onchange handler")]
    public void Custom_Switch_Should_Have_OnChange_Handler()
    {
        // Render custom Switch
        var customSwitch = RenderComponent<Switch<bool>>(parameters => parameters
            .Add(p => p.Value, false)
            .Add(p => p.Color, Color.Primary));

        // Print full markup to understand what's happening
        var markup = customSwitch.Markup;

        // The markup should contain blazor:onchange
        Assert.True(
            markup.Contains("blazor:onchange") || markup.Contains("onchange"),
            $"Switch should have onchange handler. Full markup:\n{markup}");
    }
}
