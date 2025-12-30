using System.Reflection;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginBoundaryTests
{
    private readonly PluginState _pluginState;
    private readonly PluginStateStore _stateStore;

    public PluginBoundaryTests()
    {
        _pluginState = new PluginState();
        _stateStore = new PluginStateStore(_pluginState);
    }

    #region PluginStateStore Isolation Tests

    [Fact(DisplayName = "State set by Plugin A is not visible to Plugin B")]
    public void StateStore_PluginA_NotVisibleTo_PluginB()
    {
        // Arrange
        var pluginA = CreateMockPlugin("plugin.a", typeof(PluginAType).Assembly);
        var pluginB = CreateMockPlugin("plugin.b", typeof(PluginBType).Assembly);
        _pluginState.RegisterPlugin(pluginA);
        _pluginState.RegisterPlugin(pluginB);
        
        // Act - Plugin A sets state
        _stateStore.Set("plugin.a", "counter", 42);
        
        // Assert - Plugin A can read it
        _stateStore.Get<int>("plugin.a", "counter").ShouldBe(42);
        
        // Assert - Plugin B cannot see Plugin A's state
        _stateStore.Get<int>("plugin.b", "counter").ShouldBe(0); // default int
    }

    [Fact(DisplayName = "Plugins can use the same key names without conflict")]
    public void StateStore_SameKeyNames_NoConflict()
    {
        // Arrange
        var pluginA = CreateMockPlugin("plugin.a", typeof(PluginAType).Assembly);
        var pluginB = CreateMockPlugin("plugin.b", typeof(PluginBType).Assembly);
        _pluginState.RegisterPlugin(pluginA);
        _pluginState.RegisterPlugin(pluginB);
        
        // Act - Both plugins set "settings" key
        _stateStore.Set("plugin.a", "settings", "A's settings");
        _stateStore.Set("plugin.b", "settings", "B's settings");
        
        // Assert - Each plugin sees its own value
        _stateStore.Get<string>("plugin.a", "settings").ShouldBe("A's settings");
        _stateStore.Get<string>("plugin.b", "settings").ShouldBe("B's settings");
    }

    [Fact(DisplayName = "Clearing one plugin's state doesn't affect another")]
    public void StateStore_ClearPlugin_DoesNotAffectOthers()
    {
        // Arrange
        var pluginA = CreateMockPlugin("plugin.a", typeof(PluginAType).Assembly);
        var pluginB = CreateMockPlugin("plugin.b", typeof(PluginBType).Assembly);
        _pluginState.RegisterPlugin(pluginA);
        _pluginState.RegisterPlugin(pluginB);
        
        _stateStore.Set("plugin.a", "data", "A's data");
        _stateStore.Set("plugin.b", "data", "B's data");
        
        // Act - Clear Plugin A's state
        _stateStore.ClearPlugin("plugin.a");
        
        // Assert
        _stateStore.Get<string>("plugin.a", "data").ShouldBeNull();
        _stateStore.Get<string>("plugin.b", "data").ShouldBe("B's data");
    }

    [Fact(DisplayName = "StateChanged event includes correct plugin ID")]
    public void StateStore_StateChanged_IncludesPluginId()
    {
        // Arrange
        string? capturedPluginId = null;
        string? capturedKey = null;
        _stateStore.StateChanged += (sender, args) =>
        {
            capturedPluginId = args.PluginId;
            capturedKey = args.Key;
        };
        
        // Act
        _stateStore.Set("plugin.test", "myKey", "value");
        
        // Assert
        capturedPluginId.ShouldBe("plugin.test");
        capturedKey.ShouldBe("myKey");
    }

    [Fact(DisplayName = "GetForPlugin uses assembly to infer plugin ID")]
    public void StateStore_GetForPlugin_InfersPluginId()
    {
        // Arrange
        var pluginA = CreateMockPlugin("plugin.a", typeof(PluginAType).Assembly);
        _pluginState.RegisterPlugin(pluginA);
        
        // Set directly by plugin ID
        _stateStore.Set("plugin.a", "value", 123);
        
        // Act - Get using type (simulating component access)
        var result = _stateStore.GetForPlugin<int>(typeof(PluginAType), "value");
        
        // Assert
        result.ShouldBe(123);
    }

    [Fact(DisplayName = "SetForPlugin uses assembly to infer plugin ID")]
    public void StateStore_SetForPlugin_InfersPluginId()
    {
        // Arrange
        var pluginA = CreateMockPlugin("plugin.a", typeof(PluginAType).Assembly);
        _pluginState.RegisterPlugin(pluginA);
        
        // Act - Set using type (simulating component access)
        _stateStore.SetForPlugin(typeof(PluginAType), "value", 456);
        
        // Assert - Verify via direct plugin ID access
        _stateStore.Get<int>("plugin.a", "value").ShouldBe(456);
    }

    [Fact(DisplayName = "Non-plugin type returns default for GetForPlugin")]
    public void StateStore_NonPluginType_ReturnsDefault()
    {
        // Arrange - Don't register any plugin for this assembly
        
        // Act
        var result = _stateStore.GetForPlugin<int>(typeof(string), "anything");
        
        // Assert
        result.ShouldBe(0);
    }

    #endregion

    #region PluginState Registration Tests

    [Fact(DisplayName = "Cannot register plugin with duplicate ID")]
    public void PluginState_DuplicateId_Throws()
    {
        // Arrange
        var plugin1 = CreateMockPlugin("same.id", typeof(PluginAType).Assembly);
        var plugin2 = CreateMockPlugin("same.id", typeof(PluginBType).Assembly);
        
        _pluginState.RegisterPlugin(plugin1);
        
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _pluginState.RegisterPlugin(plugin2));
    }

    [Fact(DisplayName = "Disabling plugin removes it from EnabledPlugins")]
    public void PluginState_DisablePlugin_RemovedFromEnabled()
    {
        // Arrange
        var plugin = CreateMockPlugin("test.plugin", typeof(PluginAType).Assembly);
        _pluginState.RegisterPlugin(plugin);
        
        _pluginState.EnabledPlugins.ShouldContain(plugin);
        
        // Act
        _pluginState.DisablePlugin("test.plugin");
        
        // Assert
        _pluginState.EnabledPlugins.ShouldNotContain(plugin);
        _pluginState.Plugins.ShouldContain(plugin); // Still in all plugins
    }

    [Fact(DisplayName = "PluginsActive = false disables all plugins")]
    public void PluginState_PluginsActive_False_DisablesAll()
    {
        // Arrange
        var plugin1 = CreateMockPlugin("plugin.1", typeof(PluginAType).Assembly);
        var plugin2 = CreateMockPlugin("plugin.2", typeof(PluginBType).Assembly);
        _pluginState.RegisterPlugin(plugin1);
        _pluginState.RegisterPlugin(plugin2);
        
        _pluginState.EnabledPlugins.Count().ShouldBe(2);
        
        // Act
        _pluginState.PluginsActive = false;
        
        // Assert
        _pluginState.EnabledPlugins.Count().ShouldBe(0);
        _pluginState.Plugins.Count.ShouldBe(2); // Still registered
    }

    #endregion

    #region Helper Methods

    private static PluginInfo CreateMockPlugin(string id, Assembly assembly)
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns(id);
        manifest.Name.Returns($"Test Plugin {id}");
        manifest.Version.Returns(new Version(1, 0, 0));
        manifest.Developer.Returns("Test Developer");
        manifest.Description.Returns("Test Description");
        
        return new PluginInfo
        {
            Manifest = manifest,
            Assembly = assembly,
            IsEnabled = true
        };
    }

    #endregion
}

#region Mock Plugin Types (different assemblies simulated via different classes)

// These are in the test assembly, but we use them to differentiate plugins
// In real scenarios, each plugin would be in its own assembly

internal class PluginAType { }
internal class PluginBType { }

#endregion

