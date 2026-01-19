using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Components;
using NSubstitute;
using Shouldly;
using System.Reflection;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginGuardTests : TestContext
{
    private readonly PluginState _pluginState;
    private IPluginManifest _manifest;

    public PluginGuardTests()
    {
        Services.AddMudServices();
        _pluginState = new PluginState();
        Services.AddSingleton<PluginState>(_pluginState);
        
        _manifest = Substitute.For<IPluginManifest>();
        _manifest.Id.Returns("test.plugin");
        _manifest.Name.Returns("Test Plugin");
        _manifest.Version.Returns(new Version(1, 2, 3));
        _manifest.Developer.Returns("Test Developer");
        _manifest.Description.Returns("Test Description");
    }

    private PluginInfo CreatePluginInfo(bool enabled = true)
    {
        return new PluginInfo
        {
            Manifest = _manifest,
            Assembly = typeof(PluginGuardTests).Assembly,
            IsEnabled = enabled
        };
    }

    private PluginComponentMetadata CreateMetadata()
    {
        return new PluginComponentMetadata
        {
            ComponentType = typeof(TestComponent),
            Title = "Test Component",
            Icon = Icons.Material.Filled.Extension,
            Order = 1
        };
    }

    [Fact(DisplayName = "Renders child content when plugin is enabled")]
    public async Task Renders_ChildContent_When_Plugin_Enabled()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        var metadata = CreateMetadata();

        // Act
        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .AddChildContent("<div class=\"test-content\">Hello World</div>"));

        // Assert
        cut.Find(".test-content").TextContent.ShouldBe("Hello World");
    }

    [Fact(DisplayName = "Shows disabled UI when plugin is disabled")]
    public async Task Shows_DisabledUI_When_Plugin_Disabled()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: false);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        var metadata = CreateMetadata();

        // Act
        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .AddChildContent("<div class=\"test-content\">Should not render</div>"));

        // Assert
        cut.Markup.ShouldContain("Plugin Disabled");
        cut.Markup.ShouldContain("Test Plugin");
        cut.Markup.ShouldContain("1.2.3");
        cut.FindAll(".test-content").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Shows disabled UI when PluginsActive is false")]
    public async Task Shows_DisabledUI_When_PluginsActive_False()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        _pluginState.PluginsActive = false;
        var metadata = CreateMetadata();

        // Act
        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .AddChildContent("<div class=\"test-content\">Should not render</div>"));

        // Assert
        cut.Markup.ShouldContain("Plugin Disabled");
        cut.FindAll(".test-content").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Shows deleted UI when plugin is removed")]
    public async Task Shows_DeletedUI_When_Plugin_Removed()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        var metadata = CreateMetadata();

        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .AddChildContent("<div class=\"test-content\">Should not render</div>"));

        // Act - Remove the plugin
        await _pluginState.RemovePluginAsync(pluginInfo.Manifest.Id);
        cut.Render();

        // Assert
        cut.Markup.ShouldContain("Plugin Deleted");
        cut.Markup.ShouldContain("Test Plugin");
        cut.FindAll(".test-content").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Shows error UI when child component throws exception")]
    public async Task Shows_ErrorUI_When_Component_Throws()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        var metadata = CreateMetadata();

        // Act
        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .AddChildContent<ThrowingComponent>());

        // Assert
        cut.Markup.ShouldContain("Plugin Error");
        cut.Markup.ShouldContain("Test Plugin");
        cut.Markup.ShouldContain("1.2.3");
        cut.Markup.ShouldContain("Test Component");
    }

    [Fact(DisplayName = "Shows error details when ShowErrorDetails is true")]
    public async Task Shows_ErrorDetails_When_ShowErrorDetails_True()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        var metadata = CreateMetadata();

        // Act
        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .Add(p => p.ShowErrorDetails, true)
            .AddChildContent<ThrowingComponent>());

        // Assert
        cut.Markup.ShouldContain("Error Details");
        cut.Markup.ShouldContain("Test exception");
    }

    [Fact(DisplayName = "Hides error details when ShowErrorDetails is false")]
    public async Task Hides_ErrorDetails_When_ShowErrorDetails_False()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        var metadata = CreateMetadata();

        // Act
        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .Add(p => p.ShowErrorDetails, false)
            .AddChildContent<ThrowingComponent>());

        // Assert
        cut.Markup.ShouldNotContain("Error Details");
    }

    [Fact(DisplayName = "Updates UI when plugin state changes from enabled to disabled")]
    public async Task Updates_UI_When_Plugin_Disabled()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        var metadata = CreateMetadata();

        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .AddChildContent("<div class=\"test-content\">Hello</div>"));

        cut.Find(".test-content").ShouldNotBeNull();

        // Act - Disable the plugin
        await _pluginState.DisablePluginAsync(pluginInfo.Manifest.Id);
        cut.Render();

        // Assert
        cut.Markup.ShouldContain("Plugin Disabled");
        cut.FindAll(".test-content").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Updates UI when plugin state changes from disabled to enabled")]
    public async Task Updates_UI_When_Plugin_Enabled()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: false);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        var metadata = CreateMetadata();

        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .AddChildContent("<div class=\"test-content\">Hello</div>"));

        cut.Markup.ShouldContain("Plugin Disabled");

        // Act - Enable the plugin
        await _pluginState.EnablePluginAsync(pluginInfo.Manifest.Id);
        cut.Render();

        // Assert
        cut.Find(".test-content").TextContent.ShouldBe("Hello");
        cut.Markup.ShouldNotContain("Plugin Disabled");
    }

    [Fact(DisplayName = "Updates UI when PluginsActive changes")]
    public async Task Updates_UI_When_PluginsActive_Changes()
    {
        // Arrange
        var pluginInfo = CreatePluginInfo(enabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        var metadata = CreateMetadata();

        var cut = RenderComponent<PluginGuard>(parameters => parameters
            .Add(p => p.PluginInfo, pluginInfo)
            .Add(p => p.Metadata, metadata)
            .AddChildContent("<div class=\"test-content\">Hello</div>"));

        cut.Find(".test-content").ShouldNotBeNull();

        // Act - Disable all plugins
        _pluginState.PluginsActive = false;
        cut.Render();

        // Assert
        cut.Markup.ShouldContain("Plugin Disabled");
        cut.FindAll(".test-content").Count.ShouldBe(0);
    }
}

#region Test Components

internal class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "test-component");
        builder.AddContent(2, "Test Component Content");
        builder.CloseElement();
    }
}

internal class ThrowingComponent : Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void OnInitialized()
    {
        throw new Exception("Test exception");
    }
}

#endregion

