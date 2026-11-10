using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Events;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginStateRegistrationTests
{
    private readonly PluginState _state = new();

    [Fact(DisplayName = "ReplacePluginAsync with the same version replaces without throwing")]
    public async Task ReplacePluginAsync_SameVersion_ReplacesWithoutThrowing()
    {
        var first = CreatePlugin("p", new Version(1, 0, 0));
        var second = CreatePlugin("p", new Version(1, 0, 0));
        await _state.RegisterPluginAsync(first);

        await _state.ReplacePluginAsync(second);

        _state.GetPlugin("p").ShouldBeSameAs(second);
        _state.Plugins.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "ReplacePluginAsync with an older version replaces")]
    public async Task ReplacePluginAsync_OlderVersion_Replaces()
    {
        var newer = CreatePlugin("p", new Version(2, 0, 0));
        var older = CreatePlugin("p", new Version(1, 0, 0));
        await _state.RegisterPluginAsync(newer);

        await _state.ReplacePluginAsync(older);

        _state.GetPlugin("p").ShouldBeSameAs(older);
    }

    [Fact(DisplayName = "ReplacePluginAsync keeps the disabled state")]
    public async Task ReplacePluginAsync_KeepsDisabledState()
    {
        var first = CreatePlugin("p", new Version(1, 0, 0));
        first.IsEnabled = false;
        var second = CreatePlugin("p", new Version(1, 1, 0));
        await _state.RegisterPluginAsync(first);

        await _state.ReplacePluginAsync(second);

        second.IsEnabled.ShouldBeFalse();
    }

    [Fact(DisplayName = "ReplacePluginAsync returns the replaced registration")]
    public async Task ReplacePluginAsync_ReturnsReplacedRegistration()
    {
        var first = CreatePlugin("p", new Version(1, 0, 0));
        await _state.RegisterPluginAsync(first);

        var replaced = await _state.ReplacePluginAsync(CreatePlugin("p", new Version(1, 0, 0)));

        replaced.ShouldBeSameAs(first);
    }

    [Fact(DisplayName = "ReplacePluginAsync keeps the replaced plugin's place in the list")]
    public async Task ReplacePluginAsync_KeepsListOrder()
    {
        await _state.RegisterPluginAsync(CreatePlugin("a", new Version(1, 0, 0)));
        await _state.RegisterPluginAsync(CreatePlugin("b", new Version(1, 0, 0)));
        await _state.RegisterPluginAsync(CreatePlugin("c", new Version(1, 0, 0)));
        var replacement = CreatePlugin("b", new Version(1, 1, 0));

        await _state.ReplacePluginAsync(replacement);

        _state.Plugins.Select(p => p.Manifest.Id).ShouldBe(["a", "b", "c"]);
        _state.Plugins[1].ShouldBeSameAs(replacement);
    }

    [Fact(DisplayName = "ReplacePluginAsync with no existing plugin registers and returns null")]
    public async Task ReplacePluginAsync_NoExisting_RegistersAndReturnsNull()
    {
        var plugin = CreatePlugin("p", new Version(1, 0, 0));
        var bus = Substitute.For<IMessageBus>();
        _state.SetMessageBus(bus);

        var replaced = await _state.ReplacePluginAsync(plugin);

        replaced.ShouldBeNull();
        _state.GetPlugin("p").ShouldBeSameAs(plugin);
        await bus.Received(1).PublishAsync(Arg.Is<PluginLoaded>(m => m.Plugin == plugin));
    }

    [Fact(DisplayName = "UnregisterPluginAsync removes the registration but keeps storage")]
    public async Task UnregisterPluginAsync_KeepsStorage()
    {
        var factory = Substitute.For<IPluginStorageFactory>();
        _state.SetStorageFactory(factory);
        var plugin = CreatePlugin("p", new Version(1, 0, 0));
        await _state.RegisterPluginAsync(plugin);

        var removed = await _state.UnregisterPluginAsync("p");

        removed.ShouldBeSameAs(plugin);
        _state.GetPlugin("p").ShouldBeNull();
        await factory.DidNotReceive().DeletePluginDataAsync(Arg.Any<string>());
    }

    [Fact(DisplayName = "GetPluginForComponent finds the owner of menu and panel types")]
    public async Task GetPluginForComponent_MenuAndPanelTypes_FindOwner()
    {
        var owner = CreatePlugin("p", new Version(1, 0, 0), [typeof(MenuType)], [typeof(PanelType)]);
        await _state.RegisterPluginAsync(owner);
        await _state.RegisterPluginAsync(CreatePlugin("q", new Version(1, 0, 0)));

        _state.GetPluginForComponent(typeof(MenuType)).ShouldBeSameAs(owner);
        _state.GetPluginForComponent(typeof(PanelType)).ShouldBeSameAs(owner);
    }

    [Fact(DisplayName = "GetPluginForComponent returns null for an unknown type")]
    public async Task GetPluginForComponent_UnknownType_ReturnsNull()
    {
        await _state.RegisterPluginAsync(CreatePlugin("p", new Version(1, 0, 0), [typeof(MenuType)], []));

        _state.GetPluginForComponent(typeof(PanelType)).ShouldBeNull();
    }

    private static PluginInfo CreatePlugin(string id, Version version, IReadOnlyList<Type>? menus = null, IReadOnlyList<Type>? panels = null)
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns(id);
        manifest.Name.Returns($"Test Plugin {id}");
        manifest.Version.Returns(version);
        manifest.Developer.Returns("Test Developer");
        manifest.Description.Returns("Test Description");

        return new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(PluginStateRegistrationTests).Assembly,
            IsEnabled = true,
            MenuComponents = menus ?? [],
            ContextPanelComponents = panels ?? []
        };
    }

    private sealed class MenuType;
    private sealed class PanelType;
}
