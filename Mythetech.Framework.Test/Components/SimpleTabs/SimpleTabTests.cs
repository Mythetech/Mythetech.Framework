using Bunit;
using MudBlazor.Services;
using Shouldly;

namespace Mythetech.Framework.Test.Components.SimpleTabs;

public class SimpleTabTests : BunitContext
{
    public SimpleTabTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Can_Render_Tabs()
    {
        // Act
        var cut = Render<TestTabComponent>();
        
        // Assert
        var tabs = cut.FindAll(".mud-link");
        Assert.Equal(2, tabs.Count);
        
        var tabContents = cut.Find(".px-4").TextContent;
        Assert.Contains("Tab1", tabContents);
        Assert.DoesNotContain("Tab2", tabContents);
        
        // Act
        tabs[1].Click();
        
        // Assert
        tabContents = cut.Find(".px-4").TextContent;
        Assert.DoesNotContain("Tab1", tabContents);
        Assert.Contains("Tab2", tabContents);
    }

    [Fact]
    public async Task Rebuilds_an_inactive_tab_by_default()
    {
        var cut = Render<TestTabComponent>();
        var first = cut.Find(".panel-instance").TextContent;

        await cut.FindAll(".mud-link")[1].ClickAsync(new());
        cut.FindAll(".panel-instance").ShouldBeEmpty();
        await cut.FindAll(".mud-link")[0].ClickAsync(new());

        cut.Find(".panel-instance").TextContent.ShouldNotBe(first);
    }

    [Fact]
    public async Task Keeps_an_inactive_tab_alive_and_hidden_when_asked()
    {
        var cut = Render<TestTabComponent>(p => p.Add(x => x.KeepPanelsAlive, true));
        var first = cut.Find(".panel-instance").TextContent;

        await cut.FindAll(".mud-link")[1].ClickAsync(new());
        cut.Find(".panel-instance").ParentElement!.ClassList.ShouldContain("d-none");
        await cut.FindAll(".mud-link")[0].ClickAsync(new());

        cut.Find(".panel-instance").TextContent.ShouldBe(first);
        cut.Find(".panel-instance").ParentElement!.ClassList.ShouldNotContain("d-none");
    }

    [Fact]
    public void Drops_the_content_padding_without_gutters()
    {
        var cut = Render<TestTabComponent>(p => p.Add(x => x.Gutters, false));

        cut.FindAll(".px-4").ShouldBeEmpty();
    }
}
