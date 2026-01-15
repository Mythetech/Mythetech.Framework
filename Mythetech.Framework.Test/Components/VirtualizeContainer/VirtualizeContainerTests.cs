using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using Mythetech.Framework.Components;
using Mythetech.Framework.Components.VirtualizeContainer;
using Shouldly;

namespace Mythetech.Framework.Test.Components.VirtualizeContainer;

public class VirtualizeContainerTests : TestContext
{
    public VirtualizeContainerTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "VirtualizeContainer renders container with vertical class by default")]
    public void VirtualizeContainer_RendersContainerWithVerticalClass()
    {
        // Arrange & Act
        var cut = RenderComponent<VirtualizeContainer<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1", "Item 2" })
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert
        var container = cut.Find(".mf-virtualize-container");
        container.ShouldNotBeNull();
        container.ClassList.ShouldContain("mf-virtualize-container--vertical");
    }

    [Fact(DisplayName = "VirtualizeContainer renders with horizontal class when orientation is horizontal")]
    public void VirtualizeContainer_RendersWithHorizontalClass_WhenOrientationIsHorizontal()
    {
        // Arrange & Act
        var cut = RenderComponent<VirtualizeContainer<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1", "Item 2" })
            .Add(p => p.Orientation, Orientation.Horizontal)
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert
        var container = cut.Find(".mf-virtualize-container");
        container.ClassList.ShouldContain("mf-virtualize-container--horizontal");
        container.ClassList.ShouldNotContain("mf-virtualize-container--vertical");
    }

    [Fact(DisplayName = "VirtualizeContainer applies custom CSS class")]
    public void VirtualizeContainer_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<VirtualizeContainer<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.Class, "my-custom-class")
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert
        var container = cut.Find(".mf-virtualize-container");
        container.ClassList.ShouldContain("my-custom-class");
    }

    [Fact(DisplayName = "VirtualizeContainer applies custom style")]
    public void VirtualizeContainer_AppliesCustomStyle()
    {
        // Arrange & Act
        var cut = RenderComponent<VirtualizeContainer<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.Style, "height: 400px;")
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert
        var container = cut.Find(".mf-virtualize-container");
        container.GetAttribute("style").ShouldContain("height: 400px");
    }

    [Fact(DisplayName = "VirtualizeContainer accepts ItemSize parameter")]
    public void VirtualizeContainer_AcceptsItemSizeParameter()
    {
        // Arrange & Act - should not throw
        var cut = RenderComponent<VirtualizeContainer<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.ItemSize, 48)
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert - just verify it renders without error
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "VirtualizeContainer accepts OverscanCount parameter")]
    public void VirtualizeContainer_AcceptsOverscanCountParameter()
    {
        // Arrange & Act - should not throw
        var cut = RenderComponent<VirtualizeContainer<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.OverscanCount, 5)
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert - just verify it renders without error
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "VirtualizeContainer renders with empty items")]
    public void VirtualizeContainer_RendersWithEmptyItems()
    {
        // Arrange & Act - should not throw
        var cut = RenderComponent<VirtualizeContainer<string>>(parameters => parameters
            .Add(p => p.Items, new List<string>())
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert - container should exist
        var container = cut.Find(".mf-virtualize-container");
        container.ShouldNotBeNull();
    }

    [Fact(DisplayName = "VirtualizeContainer renders with typed items")]
    public void VirtualizeContainer_RendersWithTypedItems()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new("Item 1", 100),
            new("Item 2", 200),
            new("Item 3", 300)
        };

        // Act
        var cut = RenderComponent<VirtualizeContainer<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.ChildContent, (RenderFragment<TestItem>)(item => builder =>
            {
                builder.AddContent(0, $"{item.Name}: {item.Price}");
            })));

        // Assert - container should exist
        var container = cut.Find(".mf-virtualize-container");
        container.ShouldNotBeNull();
    }

    [Fact(DisplayName = "VirtualizeContainer combines base class with orientation and custom class")]
    public void VirtualizeContainer_CombinesAllCssClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<VirtualizeContainer<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.Orientation, Orientation.Vertical)
            .Add(p => p.Class, "extra-class another-class")
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert
        var container = cut.Find(".mf-virtualize-container");
        container.ClassList.ShouldContain("mf-virtualize-container");
        container.ClassList.ShouldContain("mf-virtualize-container--vertical");
        container.ClassList.ShouldContain("extra-class");
        container.ClassList.ShouldContain("another-class");
    }

    private record TestItem(string Name, decimal Price);
}
