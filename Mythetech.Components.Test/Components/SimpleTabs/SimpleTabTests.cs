using Bunit;

namespace Mythetech.Components.Test.Components.SimpleTabs;

public class SimpleTabTests : TestContext
{
    public SimpleTabTests()
    {
        
    }

    [Fact]
    public void Can_Render_Tabs()
    {
        // Act
        var cut = RenderComponent<TestTabComponent>();
        
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
}