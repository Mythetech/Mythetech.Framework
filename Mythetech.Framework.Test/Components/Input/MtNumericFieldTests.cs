using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Input;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Input;

public class MtNumericFieldTests : TestContext
{
    public MtNumericFieldTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "Renders with default value")]
    public void Renders_WithDefaultValue()
    {
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, 5));

        cut.Find("input").GetAttribute("value").ShouldBe("5");
    }

    [Fact(DisplayName = "Renders with null value shows empty")]
    public void Renders_WithNullValue_ShowsEmpty()
    {
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, null));

        cut.Find("input").GetAttribute("value").ShouldBe("");
    }

    [Fact(DisplayName = "Increment increases value by step")]
    public async Task Increment_IncreasesValue_ByStep()
    {
        int? value = 5;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (int? v) => value = v)));

        await cut.Find(".mt-nf-spinner-up").ClickAsync(new());

        value.ShouldBe(6);
    }

    [Fact(DisplayName = "Decrement decreases value by step")]
    public async Task Decrement_DecreasesValue_ByStep()
    {
        int? value = 5;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (int? v) => value = v)));

        await cut.Find(".mt-nf-spinner-down").ClickAsync(new());

        value.ShouldBe(4);
    }

    [Fact(DisplayName = "Increment respects max bound")]
    public async Task Increment_RespectsMaxBound()
    {
        int? value = 10;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.Max, 10)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (int? v) => value = v)));

        await cut.Find(".mt-nf-spinner-up").ClickAsync(new());

        value.ShouldBe(10);
    }

    [Fact(DisplayName = "Decrement respects min bound")]
    public async Task Decrement_RespectsMinBound()
    {
        int? value = 0;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.Min, 0)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (int? v) => value = v)));

        await cut.Find(".mt-nf-spinner-down").ClickAsync(new());

        value.ShouldBe(0);
    }

    [Fact(DisplayName = "Custom step value is used")]
    public async Task CustomStep_IsUsed()
    {
        decimal? value = 1.0m;
        var cut = RenderComponent<MtNumericField<decimal>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.Step, 0.5m)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (decimal? v) => value = v)));

        await cut.Find(".mt-nf-spinner-up").ClickAsync(new());

        value.ShouldBe(1.5m);
    }

    [Fact(DisplayName = "Increment from null starts at min when min is set")]
    public async Task Increment_FromNull_StartsAtMin_WhenMinSet()
    {
        int? value = null;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.Min, 3)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (int? v) => value = v)));

        await cut.Find(".mt-nf-spinner-up").ClickAsync(new());

        value.ShouldBe(3);
    }

    [Fact(DisplayName = "Increment from null starts at default when no min")]
    public async Task Increment_FromNull_StartsAtDefault_WhenNoMin()
    {
        int? value = null;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (int? v) => value = v)));

        await cut.Find(".mt-nf-spinner-up").ClickAsync(new());

        value.ShouldBe(1);
    }

    [Fact(DisplayName = "Text input parses valid number")]
    public async Task TextInput_ParsesValidNumber()
    {
        int? value = 5;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (int? v) => value = v)));

        await cut.Find("input").InputAsync(new() { Value = "42" });

        value.ShouldBe(42);
    }

    [Fact(DisplayName = "Text input clamps to max")]
    public async Task TextInput_ClampsToMax()
    {
        int? value = 5;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.Max, 10)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (int? v) => value = v)));

        await cut.Find("input").InputAsync(new() { Value = "999" });

        value.ShouldBe(10);
    }

    [Fact(DisplayName = "Invalid text input does not change value")]
    public async Task InvalidTextInput_DoesNotChangeValue()
    {
        int? value = 5;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create(this, (int? v) => value = v)));

        await cut.Find("input").InputAsync(new() { Value = "abc" });

        value.ShouldBe(5);
    }

    [Fact(DisplayName = "ArrowUp key increments value")]
    public async Task ArrowUpKeyIncrementsValue()
    {
        int? result = null;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, 5)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<int?>(this, v => result = v)));

        var input = cut.Find("input");
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" }));

        result.ShouldBe(6);
    }

    [Fact(DisplayName = "ArrowDown key decrements value")]
    public async Task ArrowDownKeyDecrementsValue()
    {
        int? result = null;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, 5)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<int?>(this, v => result = v)));

        var input = cut.Find("input");
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" }));

        result.ShouldBe(4);
    }

    [Fact(DisplayName = "Clear button resets value to null")]
    public async Task ClearButtonResetsValueToNull()
    {
        int? result = 5;
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, 5)
            .Add(p => p.Clearable, true)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<int?>(this, v => result = v)));

        var clearButton = cut.Find(".mt-nf-clear");
        await cut.InvokeAsync(() => clearButton.Click());

        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Renders spinner buttons")]
    public void RendersSpinnerButtons()
    {
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, 0));

        cut.FindAll(".mt-nf-spinner-up").Count.ShouldBe(1);
        cut.FindAll(".mt-nf-spinner-down").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Renders label with fieldset")]
    public void RendersLabelWithFieldset()
    {
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Label, "Amount"));

        cut.Find(".mt-nf-label").TextContent.ShouldBe("Amount");
        cut.FindAll(".mt-nf-fieldset").Count.ShouldBe(1);
        cut.Find(".mt-numeric-field").ClassList.ShouldContain("mt-nf-has-label");
    }

    [Fact(DisplayName = "Applies focused class on focus")]
    public void AppliesFocusedClassOnFocus()
    {
        var cut = RenderComponent<MtNumericField<int>>();

        var input = cut.Find("input");
        input.Focus();

        cut.Find(".mt-numeric-field").ClassList.ShouldContain("mt-nf-focused");
    }

    [Fact(DisplayName = "Applies error state class")]
    public void AppliesErrorStateClass()
    {
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Error, true)
            .Add(p => p.ErrorText, "Required"));

        cut.Find(".mt-numeric-field").ClassList.ShouldContain("mt-nf-error-state");
        cut.Find(".mt-nf-error").TextContent.ShouldBe("Required");
    }

    [Fact(DisplayName = "Applies disabled class and disables inputs")]
    public void AppliesDisabledClass()
    {
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Disabled, true));

        cut.Find(".mt-numeric-field").ClassList.ShouldContain("mt-nf-disabled");
        cut.Find("input").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Renders start adornment")]
    public void RendersStartAdornment()
    {
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Adornment, Adornment.Start)
            .Add(p => p.AdornmentIcon, Icons.Material.Filled.AttachMoney));

        cut.FindAll(".mt-nf-adornment-start").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Renders clear button when clearable and has value")]
    public void RendersClearButton()
    {
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Value, 5)
            .Add(p => p.Clearable, true));

        cut.FindAll(".mt-nf-clear").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Does not render clear button when no value")]
    public void DoesNotRenderClearButtonWhenNoValue()
    {
        var cut = RenderComponent<MtNumericField<int>>(parameters => parameters
            .Add(p => p.Clearable, true));

        cut.FindAll(".mt-nf-clear").Count.ShouldBe(0);
    }
}
