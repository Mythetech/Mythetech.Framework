using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework;
using Mythetech.Framework.Components.Settings;
using Mythetech.Framework.Components.Settings.Editors;
using Mythetech.Framework.Infrastructure.Settings;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Settings;

public class DynamicSettingControlTests : BunitContext
{
    private const string StackedClass = "mf-dynamic-setting-control--stacked";

    public DynamicSettingControlTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var registry = new SettingsEditorRegistry();
        registry.RegisterEditor(typeof(string), typeof(StringSettingEditor));
        Services.AddSingleton<ISettingsEditorRegistry>(registry);
    }

    private class TestSettings : SettingsBase
    {
        public override string SettingsId => "test";
        public override string DisplayName => "Test Settings";
        public override string Icon => MythetechFrameworkIcons.Settings;

        [Setting(Label = "Name", Description = "Shown beside the label")]
        public string Name { get; set; } = "";

        [Setting(Label = "Notes", Description = "Shown beneath the label", Stacked = true)]
        public string Notes { get; set; } = "";
    }

    private IRenderedComponent<DynamicSettingControl> RenderSetting(string propertyName)
    {
        var property = typeof(TestSettings).GetProperty(propertyName)!;

        return Render<DynamicSettingControl>(parameters => parameters
            .Add(p => p.Settings, new TestSettings())
            .Add(p => p.Property, property)
            .Add(p => p.Attribute, property.GetCustomAttribute<SettingAttribute>()!));
    }

    [Fact(DisplayName = "A setting renders its editor beside the label by default")]
    public void Setting_IsNotStackedByDefault()
    {
        var cut = RenderSetting(nameof(TestSettings.Name));

        cut.Find(".mf-dynamic-setting-control").ClassList.ShouldNotContain(StackedClass);
    }

    [Fact(DisplayName = "A stacked setting is marked so its editor renders beneath the label")]
    public void StackedSetting_IsMarkedStacked()
    {
        var cut = RenderSetting(nameof(TestSettings.Notes));

        cut.Find(".mf-dynamic-setting-control").ClassList.ShouldContain(StackedClass);
    }

    [Fact(DisplayName = "A stacked setting keeps its label, description and editor")]
    public void StackedSetting_KeepsItsContent()
    {
        var cut = RenderSetting(nameof(TestSettings.Notes));

        cut.Find(".mf-setting-label").TextContent.ShouldContain("Notes");
        cut.Find(".mf-setting-description").TextContent.ShouldContain("Shown beneath the label");
        cut.FindComponent<StringSettingEditor>().ShouldNotBeNull();
    }
}
