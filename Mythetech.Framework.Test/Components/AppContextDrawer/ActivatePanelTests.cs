using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Components.AppContextDrawer.Commands;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;
using Drawer = Mythetech.Framework.Components.AppContextDrawer.AppContextDrawer;

namespace Mythetech.Framework.Test.Components.AppContextDrawer;

public class ActivatePanelTests : BunitContext
{
    public ActivatePanelTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(new PluginState());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static Task ActivateAsync(IRenderedComponent<IComponent> cut, string panelId)
    {
        var drawer = cut.FindComponent<Drawer>();
        return cut.InvokeAsync(() => drawer.Instance.Consume(new ActivatePanel(panelId)));
    }

    [Fact(DisplayName = "An ActivatePanel message opens the panel it names")]
    public async Task ActivatePanel_OpensNamedPanel()
    {
        var cut = Render<BoundPanelsTestHost>();

        await ActivateAsync(cut, "b");

        cut.Instance.IsOpen.ShouldBeTrue();
        cut.Instance.ActivePanelId.ShouldBe("b");
        cut.Markup.ShouldContain("body of b");
    }

    [Fact(DisplayName = "An ActivatePanel message consumed off the renderer's dispatcher opens the panel")]
    public async Task ActivatePanel_ConsumedOffDispatcher_OpensNamedPanel()
    {
        var cut = Render<BoundPanelsTestHost>();
        var drawer = cut.FindComponent<Drawer>();

        await Task.Run(() => drawer.Instance.Consume(new ActivatePanel("b")));

        cut.WaitForAssertion(() => cut.Instance.ActivePanelId.ShouldBe("b"));
        cut.Instance.IsOpen.ShouldBeTrue();
    }

    [Fact(DisplayName = "An ActivatePanel message for the panel that is already open leaves it open")]
    public async Task ActivatePanel_ForOpenPanel_LeavesItOpen()
    {
        var cut = Render<BoundPanelsTestHost>();
        await cut.Find("[data-panel-id=b] button").ClickAsync(new MouseEventArgs());

        await ActivateAsync(cut, "b");

        cut.Instance.IsOpen.ShouldBeTrue();
        cut.Instance.ActivePanelId.ShouldBe("b");
    }

    [Fact(DisplayName = "An ActivatePanel message opens a drawer the app collapsed with the named panel still lit")]
    public async Task ActivatePanel_ForLitPanelInCollapsedDrawer_OpensTheDrawer()
    {
        var cut = Render<BoundPanelsTestHost>();
        await cut.Find("[data-panel-id=b] button").ClickAsync(new MouseEventArgs());
        await cut.InvokeAsync(() => cut.Instance.Collapse());

        await ActivateAsync(cut, "b");

        cut.Instance.IsOpen.ShouldBeTrue();
        cut.Instance.ActivePanelId.ShouldBe("b");
        cut.Markup.ShouldContain("body of b");
    }

    [Fact(DisplayName = "An ActivatePanel message opens the panel of an item lit by its page")]
    public async Task ActivatePanel_ForItemLitByItsPage_OpensItsPanel()
    {
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("Sessions/1");
        var cut = Render<RouteMatchTestHost>();

        await ActivateAsync(cut, "session-1");

        cut.Instance.IsOpen.ShouldBeTrue();
        cut.Instance.ActivePanelId.ShouldBe("session-1");
        cut.Markup.ShouldContain("<p>session 1</p>");
    }

    [Fact(DisplayName = "An ActivatePanel message switches an open drawer to the panel of an item lit by its page")]
    public async Task ActivatePanel_ForItemLitByItsPage_SwitchesTheOpenDrawerToItsPanel()
    {
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("Sessions/1");
        var cut = Render<RouteMatchTestHost>();
        await cut.Find("[data-panel-id=sessions] button").ClickAsync(new MouseEventArgs());

        await ActivateAsync(cut, "session-1");

        cut.Instance.ActivePanelId.ShouldBe("session-1");
        cut.Markup.ShouldContain("<p>session 1</p>");
        cut.Markup.ShouldNotContain("<p>sessions</p>");
    }

    /// <summary>
    /// A loop without @key hands each item's component to whichever item now sits in its place,
    /// so the component that first rendered "b" is rendering "c" after "a" goes.
    /// </summary>
    [Fact(DisplayName = "After the first item of a list is removed, an ActivatePanel message opens the panel it names")]
    public async Task RemovedFirst_ActivatePanel_OpensNamedPanel()
    {
        var cut = Render<BoundPanelsTestHost>();
        cut.Render(p => p.Add(x => x.Ids, ["b", "c"]));

        await ActivateAsync(cut, "b");

        cut.Instance.ActivePanelId.ShouldBe("b");
        cut.Markup.ShouldContain("body of b");
        cut.Markup.ShouldNotContain("body of c");
    }

    [Fact(DisplayName = "An ActivatePanel message for an item that is no longer rendered opens nothing")]
    public async Task ActivatePanel_ForRemovedItem_OpensNothing()
    {
        var cut = Render<BoundPanelsTestHost>();
        cut.Render(p => p.Add(x => x.Ids, ["a", "b"]));

        await ActivateAsync(cut, "c");

        cut.Instance.IsOpen.ShouldBeFalse();
        cut.Instance.ActivePanelId.ShouldBeNull();
    }
}
