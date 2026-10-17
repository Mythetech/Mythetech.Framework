using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Test.Infrastructure.MessageBus;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class DisabledPluginComponentConsumerTests : BunitContext
{
    private readonly PluginState _pluginState = new();
    private readonly InMemoryMessageBus _bus;

    public DisabledPluginComponentConsumerTests()
    {
        _bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            [],
            [new DisabledPluginConsumerFilter(_pluginState)]);
        Services.AddSingleton<IMessageBus>(_bus);
    }

    [Fact(DisplayName = "A component consumer from a disabled plugin does not receive messages")]
    public async Task ComponentConsumer_FromDisabledPlugin_IsBlocked()
    {
        await RegisterTestAssemblyAsPluginAsync(isEnabled: false);
        var cut = Render<TestComponentConsumer>();

        await _bus.PublishAsync(new TestCommand("Hello"));

        cut.Find("p").TextContent.ShouldBeEmpty();
    }

    [Fact(DisplayName = "A multi-message component consumer from a disabled plugin does not receive messages")]
    public async Task MultiMessageComponentConsumer_FromDisabledPlugin_IsBlocked()
    {
        await RegisterTestAssemblyAsPluginAsync(isEnabled: false);
        var cut = Render<TestDualConsumer>();

        await _bus.PublishAsync(new TestCommand("Hello"));
        await _bus.PublishAsync(new TestCommand2(42));

        cut.Instance.ReceivedText.ShouldBeNull();
        cut.Instance.ReceivedValue.ShouldBeNull();
    }

    [Fact(DisplayName = "A multi-message component consumer from an enabled plugin receives messages")]
    public async Task MultiMessageComponentConsumer_FromEnabledPlugin_IsAllowed()
    {
        await RegisterTestAssemblyAsPluginAsync(isEnabled: true);
        var cut = Render<TestDualConsumer>();

        await _bus.PublishAsync(new TestCommand("Hello"));

        cut.Instance.ReceivedText.ShouldBe("Hello");
    }

    private Task RegisterTestAssemblyAsPluginAsync(bool isEnabled)
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("test-plugin");
        manifest.Name.Returns("Test Plugin");
        manifest.Version.Returns(new Version(1, 0, 0));

        return _pluginState.RegisterPluginAsync(new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(TestDualConsumer).Assembly,
            IsEnabled = isEnabled
        });
    }
}
