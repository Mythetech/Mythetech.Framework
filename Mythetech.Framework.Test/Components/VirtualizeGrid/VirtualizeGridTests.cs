using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using Mythetech.Framework.Components.VirtualizeGrid;
using Shouldly;

namespace Mythetech.Framework.Test.Components.VirtualizeGrid;

public class VirtualizeGridTests : TestContext
{
    public VirtualizeGridTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Setup JS module import
        JSInterop.SetupModule("./_content/Mythetech.Framework/mythetech.js");
    }

    [Fact(DisplayName = "VirtualizeGrid renders container with correct class")]
    public void VirtualizeGrid_RendersContainerWithCorrectClass()
    {
        // Arrange & Act
        var cut = RenderComponent<VirtualizeGrid<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1", "Item 2" })
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert
        var container = cut.Find(".mf-virtualize-grid");
        container.ShouldNotBeNull();
    }

    [Fact(DisplayName = "VirtualizeGrid applies custom CSS class")]
    public void VirtualizeGrid_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<VirtualizeGrid<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.Class, "custom-grid-class")
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert
        var container = cut.Find(".mf-virtualize-grid");
        container.ClassList.ShouldContain("custom-grid-class");
    }

    [Fact(DisplayName = "VirtualizeGrid applies custom style")]
    public void VirtualizeGrid_AppliesCustomStyle()
    {
        // Arrange & Act
        var cut = RenderComponent<VirtualizeGrid<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.Style, "max-height: 500px;")
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert
        var container = cut.Find(".mf-virtualize-grid");
        container.GetAttribute("style").ShouldContain("max-height: 500px");
    }

    [Fact(DisplayName = "VirtualizeGrid with empty items renders container only")]
    public void VirtualizeGrid_WithEmptyItems_RendersContainerOnly()
    {
        // Arrange & Act
        var cut = RenderComponent<VirtualizeGrid<string>>(parameters => parameters
            .Add(p => p.Items, new List<string>())
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert - container should exist but no cells rendered
        var container = cut.Find(".mf-virtualize-grid");
        container.ShouldNotBeNull();

        // No spacer should be rendered when empty
        cut.FindAll(".mf-virtualize-grid__spacer").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "VirtualizeGrid accepts ColumnCount parameter")]
    public void VirtualizeGrid_AcceptsColumnCountParameter()
    {
        // Arrange & Act - should not throw
        var cut = RenderComponent<VirtualizeGrid<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1", "Item 2", "Item 3" })
            .Add(p => p.ColumnCount, 5)
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert - just verify it renders without error
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "VirtualizeGrid accepts RowHeight parameter")]
    public void VirtualizeGrid_AcceptsRowHeightParameter()
    {
        // Arrange & Act - should not throw
        var cut = RenderComponent<VirtualizeGrid<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.RowHeight, 50)
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert - just verify it renders without error
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "VirtualizeGrid accepts ColumnWidth parameter")]
    public void VirtualizeGrid_AcceptsColumnWidthParameter()
    {
        // Arrange & Act - should not throw
        var cut = RenderComponent<VirtualizeGrid<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.ColumnWidth, 200)
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert - just verify it renders without error
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "VirtualizeGrid accepts OverscanCount parameter")]
    public void VirtualizeGrid_AcceptsOverscanCountParameter()
    {
        // Arrange & Act - should not throw
        var cut = RenderComponent<VirtualizeGrid<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1" })
            .Add(p => p.OverscanCount, 5)
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder =>
            {
                builder.AddContent(0, item);
            })));

        // Assert - just verify it renders without error
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "VirtualizeGrid renders with typed items")]
    public void VirtualizeGrid_RendersWithTypedItems()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new("Item 1", 1),
            new("Item 2", 2),
            new("Item 3", 3)
        };

        // Act
        var cut = RenderComponent<VirtualizeGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.ColumnCount, 2)
            .Add(p => p.ChildContent, (RenderFragment<TestItem>)(item => builder =>
            {
                builder.AddContent(0, $"{item.Name}-{item.Value}");
            })));

        // Assert - container should exist
        var container = cut.Find(".mf-virtualize-grid");
        container.ShouldNotBeNull();
    }

    private record TestItem(string Name, int Value);
}
