using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.AppContextDrawer;

public class LoopPluginPanelTests : BunitContext
{
    private readonly PluginState _plugins = new();

    public LoopPluginPanelTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(_plugins);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "A panel opened after a failed one from the same loop renders, not the other panel's error")]
    public async Task LoopPanels_SwitchFromThrowingToHealthy_ShowsHealthyPanel()
    {
        await RegisterAsync<ThrowingPanel>("bad", 1);
        await RegisterAsync<HealthyPanel>("good", 2);
        var cut = Render<LoopPluginPanelsTestHost>();

        await ClickAsync(cut, PanelIdOf<ThrowingPanel>());
        cut.Markup.ShouldContain("Plugin Error");

        await ClickAsync(cut, PanelIdOf<HealthyPanel>());
        cut.Markup.ShouldContain("healthy panel");
        cut.Markup.ShouldNotContain("Plugin Error");
    }

    [Fact(DisplayName = "Reloading a panel that still throws shows the error again")]
    public async Task LoopPanels_ReloadThrowingPanel_ShowsErrorAgain()
    {
        await RegisterAsync<ThrowingPanel>("bad", 1);
        var cut = Render<LoopPluginPanelsTestHost>();

        await ClickAsync(cut, PanelIdOf<ThrowingPanel>());
        cut.Markup.ShouldContain("Plugin Error");

        await cut.FindAll("button").Single(b => b.TextContent.Contains("Reload")).ClickAsync(new MouseEventArgs());

        cut.Markup.ShouldContain("Plugin Error");
    }

    [Fact(DisplayName = "A failing panel whose plugin is re-enabled from a click shows the error again")]
    public async Task LoopPanels_PluginReenabledFromClick_ShowsErrorAgain()
    {
        await RegisterAsync<ThrowingPanel>("bad", 1);
        var cut = Render<LoopPluginPanelsTestHost>();

        await ClickAsync(cut, PanelIdOf<ThrowingPanel>());
        cut.Markup.ShouldContain("Plugin Error");

        await cut.Find("#toggle-bad").ClickAsync(new MouseEventArgs());

        cut.Markup.ShouldContain("Plugin Error");
    }

    private static string PanelIdOf<TPanel>() => $"plugin-{typeof(TPanel).FullName}";

    private static Task ClickAsync(IRenderedComponent<IComponent> cut, string panelId)
        => cut.Find($"[data-panel-id=\"{panelId}\"] button").ClickAsync(new MouseEventArgs());

    private Task RegisterAsync<TPanel>(string id, int order) where TPanel : IComponent
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns(id);
        manifest.Name.Returns(id);
        manifest.Version.Returns(new Version(1, 0, 0));

        return _plugins.RegisterPluginAsync(new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(LoopPluginPanelTests).Assembly,
            IsEnabled = true,
            ContextPanelComponents = [typeof(TPanel)],
            ContextPanelComponentsMetadata =
            [
                new PluginComponentMetadata { ComponentType = typeof(TPanel), Title = id, Icon = "extension", Order = order },
            ],
        });
    }

    private sealed class ThrowingPanel : ComponentBase
    {
        protected override void OnInitialized() => throw new InvalidOperationException("boom");
    }

    private sealed class HealthyPanel : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "healthy panel");
    }
}
