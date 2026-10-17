using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Interop;
using MudBlazor.Services;
using Mythetech.Framework.Components.VirtualizeContainer;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.VirtualizeContainer;

public class VirtualizeContainerResizeTests : BunitContext
{
    private readonly IResizeObserver _observer = Substitute.For<IResizeObserver>();
    private ElementReference _observed;

    public VirtualizeContainerResizeTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _observer.Observe(Arg.Do<ElementReference>(element => _observed = element))
            .Returns(new BoundingClientRect { Width = 200, Height = 300 });
        var factory = Substitute.For<IResizeObserverFactory>();
        factory.Create().Returns(_observer);
        Services.AddSingleton(factory);
    }

    [Fact(DisplayName = "A resize that keeps the container visible does not re-render it")]
    public async Task Resize_WhileVisible_DoesNotRerender()
    {
        var cut = RenderContainer();
        var renders = cut.RenderCount;

        await ResizeAsync(cut, height: 450);

        cut.RenderCount.ShouldBe(renders);
    }

    [Fact(DisplayName = "A container resized to nothing stops rendering its items")]
    public async Task Resize_ToNothing_StopsRenderingItems()
    {
        var cut = RenderContainer();
        cut.FindComponents<Virtualize<string>>().ShouldHaveSingleItem();

        await ResizeAsync(cut, height: 0);

        cut.FindComponents<Virtualize<string>>().ShouldBeEmpty();
    }

    private IRenderedComponent<VirtualizeContainer<string>> RenderContainer()
        => Render<VirtualizeContainer<string>>(parameters => parameters
            .Add(p => p.Items, new List<string> { "Item 1", "Item 2" })
            .Add(p => p.ChildContent, (RenderFragment<string>)(item => builder => builder.AddContent(0, item))));

    private Task ResizeAsync(IRenderedComponent<VirtualizeContainer<string>> cut, double height)
        => cut.InvokeAsync(() => _observer.OnResized += Raise.Event<SizeChanged>(
            new Dictionary<ElementReference, BoundingClientRect> { [_observed] = new() { Width = 200, Height = height } }));
}
