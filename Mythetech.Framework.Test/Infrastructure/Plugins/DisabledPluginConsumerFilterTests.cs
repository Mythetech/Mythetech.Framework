using System.Reflection;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class DisabledPluginConsumerFilterTests
{
    private readonly PluginState _pluginState = new();
    private readonly DisabledPluginConsumerFilter _filter;

    public DisabledPluginConsumerFilterTests()
    {
        _filter = new DisabledPluginConsumerFilter(_pluginState);
    }

    [Fact(DisplayName = "Non-plugin consumer is always allowed")]
    public void NonPluginConsumer_IsAlwaysAllowed()
    {
        // Arrange
        var consumer = new TestConsumer();
        var message = new TestMessage("Test");
        
        // Act
        var result = _filter.ShouldInvoke(consumer, message);
        
        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Consumer from enabled plugin is allowed")]
    public async Task ConsumerFromEnabledPlugin_IsAllowed()
    {
        // Arrange
        var assembly = typeof(TestConsumer).Assembly;
        var pluginInfo = CreatePluginInfo("test-plugin", assembly, isEnabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);

        var consumer = new TestConsumer();
        var message = new TestMessage("Test");

        // Act
        var result = _filter.ShouldInvoke(consumer, message);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Consumer from disabled plugin is blocked")]
    public async Task ConsumerFromDisabledPlugin_IsBlocked()
    {
        // Arrange
        var assembly = typeof(TestConsumer).Assembly;
        var pluginInfo = CreatePluginInfo("test-plugin", assembly, isEnabled: false);
        await _pluginState.RegisterPluginAsync(pluginInfo);

        var consumer = new TestConsumer();
        var message = new TestMessage("Test");

        // Act
        var result = _filter.ShouldInvoke(consumer, message);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "Consumer from plugin is blocked when PluginsActive is false")]
    public async Task ConsumerFromPlugin_BlockedWhenPluginsInactive()
    {
        // Arrange
        var assembly = typeof(TestConsumer).Assembly;
        var pluginInfo = CreatePluginInfo("test-plugin", assembly, isEnabled: true);
        await _pluginState.RegisterPluginAsync(pluginInfo);
        _pluginState.PluginsActive = false;

        var consumer = new TestConsumer();
        var message = new TestMessage("Test");

        // Act
        var result = _filter.ShouldInvoke(consumer, message);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "Non-plugin consumer is allowed even when PluginsActive is false")]
    public void NonPluginConsumer_AllowedWhenPluginsInactive()
    {
        // Arrange
        _pluginState.PluginsActive = false;
        
        var consumer = new TestConsumer();
        var message = new TestMessage("Test");
        
        // Act
        var result = _filter.ShouldInvoke(consumer, message);
        
        // Assert
        result.ShouldBeTrue();
    }

    private static PluginInfo CreatePluginInfo(string id, Assembly assembly, bool isEnabled)
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
            IsEnabled = isEnabled
        };
    }
}

#region Test Types

public record TestMessage(string Content);

public class TestConsumer : IConsumer<TestMessage>
{
    public Task Consume(TestMessage message) => Task.CompletedTask;
}

#endregion

