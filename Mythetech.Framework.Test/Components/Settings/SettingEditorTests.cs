using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Settings.Editors;
using Mythetech.Framework.Infrastructure.Settings;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Settings;

public class SettingEditorTests : TestContext
{
    public SettingEditorTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Add MudPopoverProvider for components that use popovers (MudSelect)
        RenderComponent<MudPopoverProvider>();
    }

    #region Test Settings Model

    private class TestSettings : SettingsBase
    {
        public override string SettingsId => "test";
        public override string DisplayName => "Test Settings";
        public override string Icon => Icons.Material.Filled.Settings;

        [Setting(Label = "Bool Setting", Description = "A boolean setting")]
        public bool BoolValue { get; set; }

        [Setting(Label = "Int Setting", Description = "An integer setting")]
        public int IntValue { get; set; }

        [Setting(Label = "Int Slider Setting", Min = 0, Max = 100, Step = 5)]
        public int IntSliderValue { get; set; } = 50;

        [Setting(Label = "Double Setting", Description = "A double setting")]
        public double DoubleValue { get; set; }

        [Setting(Label = "Double Slider Setting", Min = 0.0, Max = 1.0, Step = 0.1)]
        public double DoubleSliderValue { get; set; } = 0.5;

        [Setting(Label = "String Setting", Description = "A string setting")]
        public string StringValue { get; set; } = "";

        [Setting(Label = "String Options Setting", Options = "Option1,Option2,Option3")]
        public string StringOptionsValue { get; set; } = "Option1";

        [Setting(Label = "Enum Setting")]
        public TestEnum EnumValue { get; set; }
    }

    private enum TestEnum
    {
        FirstValue,
        SecondValue,
        ThirdValue
    }

    #endregion

    #region BoolSettingEditor Tests

    [Fact(DisplayName = "BoolSettingEditor renders switch component")]
    public void BoolSettingEditor_RendersSwitch()
    {
        // Arrange
        var settings = new TestSettings { BoolValue = true };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.BoolValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<BoolSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert
        cut.Find("label.mud-switch-m3").ShouldNotBeNull();
    }

    [Fact(DisplayName = "BoolSettingEditor displays correct initial value")]
    public void BoolSettingEditor_DisplaysCorrectInitialValue()
    {
        // Arrange
        var settings = new TestSettings { BoolValue = true };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.BoolValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<BoolSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert
        var input = cut.Find("input[type='checkbox']");
        input.GetAttribute("checked").ShouldNotBeNull();
    }

    [Fact(DisplayName = "BoolSettingEditor displays false value correctly")]
    public void BoolSettingEditor_DisplaysFalseValue()
    {
        // Arrange
        var settings = new TestSettings { BoolValue = false };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.BoolValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<BoolSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert - unchecked switch should not have mud-checked class
        var switchBase = cut.Find(".mud-switch-base-m3");
        switchBase.ClassList.ShouldNotContain("mud-checked");
    }

    #endregion

    #region IntSettingEditor Tests

    [Fact(DisplayName = "IntSettingEditor renders numeric field without range")]
    public void IntSettingEditor_RendersNumericField_WithoutRange()
    {
        // Arrange
        var settings = new TestSettings { IntValue = 42 };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.IntValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<IntSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert
        cut.Find(".mf-setting-numeric").ShouldNotBeNull();
    }

    [Fact(DisplayName = "IntSettingEditor renders slider with range")]
    public void IntSettingEditor_RendersSlider_WithRange()
    {
        // Arrange
        var settings = new TestSettings { IntSliderValue = 50 };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.IntSliderValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<IntSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert
        cut.Find(".mf-setting-slider-container").ShouldNotBeNull();
        cut.Find(".mf-setting-slider-value").TextContent.ShouldBe("50");
    }

    [Fact(DisplayName = "IntSettingEditor updates value on numeric field change")]
    public void IntSettingEditor_UpdatesValue_OnNumericFieldChange()
    {
        // Arrange
        var settings = new TestSettings { IntValue = 0 };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.IntValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;
        var valueChanged = false;

        // Act
        var cut = RenderComponent<IntSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create(this, () => valueChanged = true)));

        cut.Find("input").Change("99");

        // Assert
        settings.IntValue.ShouldBe(99);
        valueChanged.ShouldBeTrue();
    }

    #endregion

    #region DoubleSettingEditor Tests

    [Fact(DisplayName = "DoubleSettingEditor renders numeric field without range")]
    public void DoubleSettingEditor_RendersNumericField_WithoutRange()
    {
        // Arrange
        var settings = new TestSettings { DoubleValue = 3.14 };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.DoubleValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<DoubleSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert
        cut.Find(".mf-setting-numeric").ShouldNotBeNull();
    }

    [Fact(DisplayName = "DoubleSettingEditor renders slider with range")]
    public void DoubleSettingEditor_RendersSlider_WithRange()
    {
        // Arrange
        var settings = new TestSettings { DoubleSliderValue = 0.5 };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.DoubleSliderValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<DoubleSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert
        cut.Find(".mf-setting-slider-container").ShouldNotBeNull();
        cut.Find(".mf-setting-slider-value").TextContent.ShouldBe("0.5");
    }

    [Fact(DisplayName = "DoubleSettingEditor updates value on numeric field change")]
    public void DoubleSettingEditor_UpdatesValue_OnNumericFieldChange()
    {
        // Arrange
        var settings = new TestSettings { DoubleValue = 0.0 };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.DoubleValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;
        var valueChanged = false;

        // Act
        var cut = RenderComponent<DoubleSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create(this, () => valueChanged = true)));

        cut.Find("input").Change("2.5");

        // Assert
        settings.DoubleValue.ShouldBe(2.5);
        valueChanged.ShouldBeTrue();
    }

    #endregion

    #region StringSettingEditor Tests

    [Fact(DisplayName = "StringSettingEditor renders text field without options")]
    public void StringSettingEditor_RendersTextField_WithoutOptions()
    {
        // Arrange
        var settings = new TestSettings { StringValue = "test" };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.StringValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<StringSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert
        cut.Find(".mf-setting-text").ShouldNotBeNull();
    }

    [Fact(DisplayName = "StringSettingEditor renders select with options")]
    public void StringSettingEditor_RendersSelect_WithOptions()
    {
        // Arrange
        var settings = new TestSettings { StringOptionsValue = "Option1" };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.StringOptionsValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<StringSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert
        cut.Find(".mf-setting-select").ShouldNotBeNull();
    }

    [Fact(DisplayName = "StringSettingEditor updates value on text field change")]
    public void StringSettingEditor_UpdatesValue_OnTextFieldChange()
    {
        // Arrange
        var settings = new TestSettings { StringValue = "" };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.StringValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;
        var valueChanged = false;

        // Act
        var cut = RenderComponent<StringSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create(this, () => valueChanged = true)));

        cut.Find("input").Change("new value");

        // Assert
        settings.StringValue.ShouldBe("new value");
        valueChanged.ShouldBeTrue();
    }

    #endregion

    #region EnumSettingEditor Tests

    [Fact(DisplayName = "EnumSettingEditor renders select component")]
    public void EnumSettingEditor_RendersSelect()
    {
        // Arrange
        var settings = new TestSettings { EnumValue = TestEnum.FirstValue };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.EnumValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<EnumSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert
        cut.Find(".mf-setting-select").ShouldNotBeNull();
    }

    [Fact(DisplayName = "EnumSettingEditor displays correct initial value")]
    public void EnumSettingEditor_DisplaysCorrectInitialValue()
    {
        // Arrange
        var settings = new TestSettings { EnumValue = TestEnum.SecondValue };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.EnumValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<EnumSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert - MudSelect shows value in input
        cut.Find("input").GetAttribute("value").ShouldBe("SecondValue");
    }

    [Fact(DisplayName = "EnumSettingEditor renders with correct CSS class")]
    public void EnumSettingEditor_RendersWithCorrectClass()
    {
        // Arrange
        var settings = new TestSettings { EnumValue = TestEnum.FirstValue };
        var property = typeof(TestSettings).GetProperty(nameof(TestSettings.EnumValue))!;
        var attribute = property.GetCustomAttribute<SettingAttribute>()!;

        // Act
        var cut = RenderComponent<EnumSettingEditor>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, attribute));

        // Assert - verify the select has the correct class and initial value
        var select = cut.Find(".mf-setting-select");
        select.ShouldNotBeNull();
        cut.Find("input").GetAttribute("value").ShouldBe("FirstValue");
    }

    #endregion
}
