using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.Commands;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Components;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginMenuHostTests : BunitContext
{
    private readonly PluginState _plugins = new();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();

    public PluginMenuHostTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_plugins);
        Services.AddSingleton(_bus);
        Services.AddSingleton(new PluginMenuItemInvoker(_bus, NullLogger<PluginMenuItemInvoker>.Instance));
        Services.AddScoped(_ => new PluginContext { MessageBus = _bus, StateStore = new PluginStateStore(_plugins) });
    }

    private Task RegisterAsync<TMenu>() where TMenu : PluginMenu
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("notes");
        manifest.Name.Returns("Notes");
        manifest.Version.Returns(new Version(1, 0, 0));

        return _plugins.RegisterPluginAsync(new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(PluginMenuHostTests).Assembly,
            MenuComponents = [typeof(TMenu)],
            MenuComponentsMetadata =
            [
                new PluginComponentMetadata { ComponentType = typeof(TMenu), Title = "Notes", Icon = "extension" },
            ],
        });
    }

    [Fact(DisplayName = "A menu component that throws while rendering shows the plugin error UI")]
    public async Task PluginMenuHost_MenuComponentThrows_ShowsError()
    {
        await RegisterAsync<ThrowingRenderMenu>();
        Render<MudPopoverProvider>();

        var cut = Render<PluginMenuHost>(p => p.Add(x => x.MenuType, typeof(ThrowingRenderMenu)));

        cut.Render();
        cut.WaitForAssertion(() => cut.FindComponent<PluginGuard>().Markup.ShouldContain("Plugin Error"));
    }

    [Fact(DisplayName = "A menu item click that throws is contained and shown as a notification")]
    public async Task PluginMenuHost_ItemClickThrows_DoesNotThrowAndNotifies()
    {
        await RegisterAsync<ThrowingClickMenu>();
        var cut = Render<PluginMenuItemsRenderer>(p => p
            .Add(x => x.MenuType, typeof(ThrowingClickMenu))
            .Add(x => x.PluginContext, new PluginContext { MessageBus = _bus, StateStore = new PluginStateStore(_plugins) }));
        cut.WaitForState(() => cut.FindAll(".mud-menu-item").Count > 0);

        await Should.NotThrowAsync(() => cut.Find(".mud-menu-item").ClickAsync());

        await _bus.Received(1).PublishAsync(Arg.Is<AddNotification>(n =>
            n.Severity == Severity.Error && n.Message == "Notes couldn't complete that action."));
    }

    [Fact(DisplayName = "A host menu item click that throws is contained and shown as a notification")]
    public async Task PluginMenuHost_HostItemClickThrows_DoesNotThrowAndNotifies()
    {
        await RegisterAsync<ThrowingClickMenu>();
        var provider = Render<MudPopoverProvider>();
        var cut = Render<PluginMenuHost>(p => p.Add(x => x.MenuType, typeof(ThrowingClickMenu)));
        await cut.Find(".mud-menu > div, div[class*='mud-menu']").TriggerEventAsync("onpointerenter", new Microsoft.AspNetCore.Components.Web.PointerEventArgs());
        provider.WaitForState(() => provider.FindAll(".mud-menu-item").Any(i => i.TextContent.Contains("Do it")));

        await Should.NotThrowAsync(() => provider.FindAll(".mud-menu-item").First(i => i.TextContent.Contains("Do it")).ClickAsync());

        await _bus.Received(1).PublishAsync(Arg.Is<AddNotification>(n =>
            n.Message == "Notes couldn't complete that action."));
    }

    private sealed class ThrowingRenderMenu : PluginMenu
    {
        public override string Icon => "extension";
        public override string Title => "Notes";

        protected override void OnInitialized() => throw new InvalidOperationException("boom");
    }

    private sealed class ThrowingClickMenu : PluginMenu
    {
        public override string Icon => "extension";
        public override string Title => "Notes";

        public override PluginMenuItem[]? Items =>
        [
            new PluginMenuItem
            {
                Icon = "extension",
                Text = "Do it",
                OnClick = _ => throw new InvalidOperationException("boom"),
            },
        ];
    }
}
