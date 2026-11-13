using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Interop;
using MudBlazor.Services;
using Mythetech.Framework.Components.SimpleTabs;
using NSubstitute;
using Shouldly;
using SimpleTabsComponent = Mythetech.Framework.Components.SimpleTabs.SimpleTabs;

namespace Mythetech.Framework.Test.Components.SimpleTabs;

public class SimpleTabsCompactTests : BunitContext
{
    public SimpleTabsCompactTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var observer = Substitute.For<IResizeObserver>();
        observer.Observe(Arg.Any<ElementReference>()).Returns(new BoundingClientRect { Width = 300, Height = 40 });
        Services.AddSingleton(observer);
    }

    [Fact(DisplayName = "The compact overflow menu pads its list by default")]
    public void Compact_menu_list_is_padded_by_default()
    {
        var cut = RenderTabs();

        cut.FindComponent<MudMenu>().Instance.ListClass.ShouldBe("pa-1");
    }

    [Fact(DisplayName = "The compact overflow menu takes a custom list class")]
    public void Compact_menu_list_takes_a_custom_class()
    {
        var cut = RenderTabs(p => p.Add(x => x.ListClass, "pa-3"));

        cut.FindComponent<MudMenu>().Instance.ListClass.ShouldBe("pa-3");
    }

    private IRenderedComponent<SimpleTabsComponent> RenderTabs(
        Action<ComponentParameterCollectionBuilder<SimpleTabsComponent>>? configure = null)
        => Render<SimpleTabsComponent>(p =>
        {
            p.AddChildContent<Tab>(t => t.Add(x => x.Name, "One").AddChildContent("One"));
            p.AddChildContent<Tab>(t => t.Add(x => x.Name, "Two").AddChildContent("Two"));
            configure?.Invoke(p);
        });
}
