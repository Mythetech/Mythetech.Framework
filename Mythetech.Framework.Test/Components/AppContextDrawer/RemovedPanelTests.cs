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

public class RemovedPanelTests : BunitContext
{
    private readonly PluginState _plugins = new();

    public RemovedPanelTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(_plugins);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static Task ClickAsync(IRenderedComponent<IComponent> cut, string panelId)
        => cut.Find($"[data-panel-id=\"{panelId}\"] button").ClickAsync(new MouseEventArgs());

    [Fact(DisplayName = "When the open panel's item leaves the page, the drawer shows the empty state")]
    public async Task OpenItemRemoved_ShowsEmptyState()
    {
        var cut = Render<KeyedPanelsTestHost>();
        await ClickAsync(cut, "b");

        cut.Render(p => p.Add(x => x.Ids, ["a", "c"]));

        cut.Markup.ShouldContain("No Item Selected");
        cut.Markup.ShouldNotContain("body of b");
    }

    [Fact(DisplayName = "When the open panel's item leaves the page, the drawer stays open with no panel active")]
    public async Task OpenItemRemoved_StaysOpenWithNoActivePanel()
    {
        var cut = Render<KeyedPanelsTestHost>();
        await ClickAsync(cut, "b");

        cut.Render(p => p.Add(x => x.Ids, ["a", "c"]));

        cut.WaitForAssertion(() => cut.Instance.ActivePanelId.ShouldBeNull());
        cut.Instance.IsOpen.ShouldBeTrue();
    }

    [Fact(DisplayName = "When another item leaves the page, the open panel stays")]
    public async Task OtherItemRemoved_KeepsOpenPanel()
    {
        var cut = Render<KeyedPanelsTestHost>();
        await ClickAsync(cut, "b");

        cut.Render(p => p.Add(x => x.Ids, ["b", "c"]));

        cut.Markup.ShouldContain("body of b");
        cut.Instance.ActivePanelId.ShouldBe("b");
    }

    /// <summary>
    /// A loop without @key drops the last component when an earlier item goes, and hands the open
    /// item's id to the component before it. The panel belongs to the id, so it stays.
    /// </summary>
    [Fact(DisplayName = "When a list without keys loses an earlier item, the open panel of the last item stays")]
    public async Task KeylessList_EarlierItemRemoved_KeepsOpenPanelOfLastItem()
    {
        var cut = Render<BoundPanelsTestHost>(p => p.Add(x => x.Ids, ["a", "b", "c"]));
        await ClickAsync(cut, "c");

        cut.Render(p => p.Add(x => x.Ids, ["b", "c"]));
        cut.Render();

        cut.Markup.ShouldContain("body of c");
        cut.Markup.ShouldNotContain("No Item Selected");
        cut.Instance.ActivePanelId.ShouldBe("c");
    }

    [Fact(DisplayName = "When a list without keys loses its open last item, the drawer shows the empty state")]
    public async Task KeylessList_OpenLastItemRemoved_ShowsEmptyState()
    {
        var cut = Render<BoundPanelsTestHost>(p => p.Add(x => x.Ids, ["a", "b", "c"]));
        await ClickAsync(cut, "c");

        cut.Render(p => p.Add(x => x.Ids, ["a", "b"]));

        cut.Markup.ShouldContain("No Item Selected");
        cut.Markup.ShouldNotContain("body of c");
    }

    [Fact(DisplayName = "When a plugin above the open plugin panel is disabled, the open panel stays")]
    public async Task PluginAboveOpenPanelDisabled_KeepsOpenPanel()
    {
        await RegisterPluginAsync<FirstPluginPanel>("first", order: 1);
        await RegisterPluginAsync<SecondPluginPanel>("second", order: 2);
        await RegisterPluginAsync<ThirdPluginPanel>("third", order: 3);
        var cut = Render<PluginPanelsTestHost>();
        await ClickAsync(cut, PanelIdOf<SecondPluginPanel>());
        cut.Markup.ShouldContain("second plugin panel");

        await cut.InvokeAsync(() => _plugins.DisablePluginAsync("first"));
        cut.Render();

        cut.Markup.ShouldContain("second plugin panel");
        cut.Markup.ShouldNotContain("third plugin panel");
        cut.Instance.ActivePanelId.ShouldBe(PanelIdOf<SecondPluginPanel>());
    }

    private static string PanelIdOf<TPanel>() => $"plugin-{typeof(TPanel).FullName}";

    private Task RegisterPluginAsync<TPanel>(string id, int order) where TPanel : IComponent
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns(id);
        manifest.Name.Returns(id);
        manifest.Version.Returns(new Version(1, 0, 0));

        return _plugins.RegisterPluginAsync(new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(RemovedPanelTests).Assembly,
            IsEnabled = true,
            ContextPanelComponentsMetadata =
            [
                new PluginComponentMetadata { ComponentType = typeof(TPanel), Title = id, Icon = "extension", Order = order },
            ],
        });
    }

    private sealed class FirstPluginPanel : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "first plugin panel");
    }

    private sealed class SecondPluginPanel : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "second plugin panel");
    }

    private sealed class ThirdPluginPanel : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "third plugin panel");
    }
}
