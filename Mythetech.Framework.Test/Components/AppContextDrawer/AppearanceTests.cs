using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Buttons;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;
using Drawer = Mythetech.Framework.Components.AppContextDrawer.AppContextDrawer;
using Item = Mythetech.Framework.Components.AppContextDrawer.ContextPanelItem;

namespace Mythetech.Framework.Test.Components.AppContextDrawer;

public class AppearanceTests : BunitContext
{
    public AppearanceTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(new PluginState());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<AppearanceTestHost> RenderDrawer(params (string Name, object Value)[] parameters)
        => Render<AppearanceTestHost>(p => p.Add(x => x.Drawer, parameters.ToDictionary(x => x.Name, x => x.Value)));

    /// <summary>
    /// The stack that holds the header and the panel content: the only stack in the drawer that fills the height.
    /// </summary>
    private static MudStack ContentStack(IRenderedComponent<AppearanceTestHost> cut)
        => cut.FindComponents<MudStack>().Select(s => s.Instance).Single(s => (s.Class ?? "").Contains("h-100"));

    private static MudStack HeaderRow(IRenderedComponent<AppearanceTestHost> cut)
        => cut.FindComponents<MudStack>().Select(s => s.Instance).Single(s => s.Row && s.AlignItems == AlignItems.Center);

    private static MudIcon HeaderIcon(IRenderedComponent<AppearanceTestHost> cut)
        => cut.FindComponents<MudIcon>().Select(i => i.Instance).Single(i => i.Color == Color.Primary && i.Icon == "extension");

    private static MudTooltip CloseTooltip(IRenderedComponent<AppearanceTestHost> cut)
        => cut.FindComponents<MudTooltip>().Select(t => t.Instance).Single(t => t.Text == "Close");

    [Fact(DisplayName = "By default the content keeps its padding and the gap under the header")]
    public void Content_DefaultsToPaddedAndSpaced()
    {
        var cut = RenderDrawer();

        ContentStack(cut).Class.ShouldBe("pa-4 w-100 h-100");
        ContentStack(cut).Spacing.ShouldBe(3);
    }

    [Fact(DisplayName = "ContentClass replaces the content padding")]
    public void ContentClass_ReplacesPadding()
    {
        var cut = RenderDrawer((nameof(Drawer.ContentClass), "pa-1"));

        ContentStack(cut).Class.ShouldBe("pa-1 w-100 h-100");
    }

    [Fact(DisplayName = "A blank ContentClass leaves the content without padding")]
    public void BlankContentClass_LeavesNoPadding()
    {
        var cut = RenderDrawer((nameof(Drawer.ContentClass), ""));

        ContentStack(cut).Class.ShouldBe("w-100 h-100");
    }

    [Fact(DisplayName = "ContentSpacing sets the gap under the header")]
    public void ContentSpacing_SetsGap()
    {
        var cut = RenderDrawer((nameof(Drawer.ContentSpacing), 0));

        ContentStack(cut).Spacing.ShouldBe(0);
    }

    [Fact(DisplayName = "By default the header has no spacing and its icon keeps its margin")]
    public void Header_DefaultsToIconMargin()
    {
        var cut = RenderDrawer();

        HeaderRow(cut).Class.ShouldBe("w-100");
        HeaderRow(cut).Spacing.ShouldBe(0);
        HeaderIcon(cut).Class.ShouldBe("mr-2");
    }

    [Fact(DisplayName = "HeaderClass is added to the header row")]
    public void HeaderClass_IsAdded()
    {
        var cut = RenderDrawer((nameof(Drawer.HeaderClass), "pa-2 mb-2"));

        HeaderRow(cut).Class.ShouldBe("w-100 pa-2 mb-2");
    }

    [Fact(DisplayName = "HeaderSpacing spaces the header evenly and drops the icon's margin")]
    public void HeaderSpacing_SpacesEvenly()
    {
        var cut = RenderDrawer((nameof(Drawer.HeaderSpacing), 3));

        HeaderRow(cut).Spacing.ShouldBe(3);
        HeaderIcon(cut).Class.ShouldBeNullOrEmpty();
    }

    [Fact(DisplayName = "A HeaderSpacing of zero is still a spacing, so the icon's margin goes")]
    public void HeaderSpacingZero_DropsIconMargin()
    {
        var cut = RenderDrawer((nameof(Drawer.HeaderSpacing), 0));

        HeaderRow(cut).Spacing.ShouldBe(0);
        HeaderIcon(cut).Class.ShouldBeNullOrEmpty();
    }

    [Fact(DisplayName = "By default an open drawer with no panel shows the built-in empty state")]
    public void EmptyState_DefaultsToBuiltIn()
    {
        var cut = Render<AppearanceTestHost>(p => p.Add(x => x.ActivePanelId, null));

        cut.Markup.ShouldContain("No Item Selected");
    }

    [Fact(DisplayName = "EmptyContent replaces the built-in empty state")]
    public void EmptyContent_ReplacesBuiltIn()
    {
        RenderFragment empty = builder => builder.AddMarkupContent(0, "<p>pick something</p>");

        var cut = Render<AppearanceTestHost>(p => p
            .Add(x => x.ActivePanelId, null)
            .Add(x => x.Drawer, new Dictionary<string, object> { [nameof(Drawer.EmptyContent)] = empty }));

        cut.Markup.ShouldContain("pick something");
        cut.Markup.ShouldNotContain("No Item Selected");
    }

    [Fact(DisplayName = "EmptyContent is not shown while a panel is active")]
    public void EmptyContent_HiddenWhilePanelActive()
    {
        RenderFragment empty = builder => builder.AddMarkupContent(0, "<p>pick something</p>");

        var cut = RenderDrawer((nameof(Drawer.EmptyContent), empty));

        cut.Markup.ShouldContain("first panel body");
        cut.Markup.ShouldNotContain("pick something");
    }

    [Fact(DisplayName = "By default rail items do not fill the rail")]
    public void Items_DefaultToButtonSized()
    {
        var cut = RenderDrawer();

        cut.FindAll(".context-panel-item-wrapper").Count.ShouldBe(2);
        cut.FindAll(".context-panel-item-wrapper--fill").ShouldBeEmpty();
    }

    [Fact(DisplayName = "FullWidthItems marks every rail item to fill the rail")]
    public void FullWidthItems_MarksEveryItem()
    {
        var cut = RenderDrawer((nameof(Drawer.FullWidthItems), true));

        cut.FindAll(".context-panel-item-wrapper--fill").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "FullWidthItems also reaches the drawer's built-in plugin panels")]
    public async Task FullWidthItems_ReachesPluginPanels()
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("test.plugin");
        manifest.Name.Returns("Test Plugin");
        manifest.Version.Returns(new Version(1, 0, 0));
        await Services.GetRequiredService<PluginState>().RegisterPluginAsync(new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(AppearanceTests).Assembly,
            IsEnabled = true,
            ContextPanelComponentsMetadata =
            [
                new PluginComponentMetadata
                {
                    ComponentType = typeof(Infrastructure.Plugins.TestComponent),
                    Title = "Plugin Panel",
                    Icon = "extension",
                    Order = 1,
                },
            ],
        });

        var cut = RenderDrawer((nameof(Drawer.FullWidthItems), true), (nameof(Drawer.ShowPluginPanels), true));

        cut.FindAll(".context-panel-item-wrapper--fill").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "By default an inactive rail item keeps the surface colour and a primary tooltip")]
    public void InactiveItem_DefaultColours()
    {
        var cut = RenderDrawer();

        var inactive = cut.FindComponent<MtIconButton>().Instance;
        inactive.Color.ShouldBe(Color.Surface);
        inactive.TooltipColor.ShouldBe(Color.Primary);
        CloseTooltip(cut).Color.ShouldBe(Color.Primary);
    }

    [Fact(DisplayName = "A rail item rendered outside a drawer keeps the default look")]
    public void ItemOutsideDrawer_UsesDefaults()
    {
        Render<MudPopoverProvider>();

        var cut = Render<Item>(p => p
            .Add(x => x.Id, "solo")
            .Add(x => x.Icon, "extension")
            .Add(x => x.Title, "Solo"));

        cut.Find(".context-panel-item-wrapper").ClassList.ShouldNotContain("context-panel-item-wrapper--fill");
        var button = cut.FindComponent<MtIconButton>().Instance;
        button.Color.ShouldBe(Color.Surface);
        button.TooltipColor.ShouldBe(Color.Primary);
    }

    [Fact(DisplayName = "ItemColor colours inactive rail items only")]
    public void ItemColor_ColoursInactiveItems()
    {
        var cut = RenderDrawer((nameof(Drawer.ItemColor), Color.Inherit));

        cut.FindComponent<MtIconButton>().Instance.Color.ShouldBe(Color.Inherit);
        cut.FindComponents<MudIconButton>().Select(b => b.Instance)
            .Single(b => (b.Class ?? "").Contains("context-item-active")).Color.ShouldBe(Color.Primary);
    }

    [Fact(DisplayName = "TooltipColor colours the rail tooltips and the close tooltip")]
    public void TooltipColor_ColoursTooltips()
    {
        var cut = RenderDrawer((nameof(Drawer.TooltipColor), Color.Default));

        cut.FindComponent<MtIconButton>().Instance.TooltipColor.ShouldBe(Color.Default);
        CloseTooltip(cut).Color.ShouldBe(Color.Default);
    }
}
