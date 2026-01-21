using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure;

/// <summary>
/// Tests for thread safety of core infrastructure components under concurrent load.
/// </summary>
[Trait("Category", "ThreadSafety")]
public class ThreadSafetyTests : TestContext
{
    #region MessageBus Concurrent Tests

    [Fact(DisplayName = "MessageBus handles concurrent publishes safely")]
    public async Task MessageBus_ConcurrentPublish_AllMessagesDelivered()
    {
        // Arrange
        var bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        var receivedCount = 0;
        var countingConsumer = new CountingConsumer(() => Interlocked.Increment(ref receivedCount));
        bus.Subscribe(countingConsumer);

        const int messageCount = 100;

        // Act - publish 100 messages concurrently
        var tasks = Enumerable.Range(0, messageCount)
            .Select(i => bus.PublishAsync(new ConcurrentTestMessage(i)))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - all messages should be delivered
        receivedCount.ShouldBe(messageCount);
    }

    [Fact(DisplayName = "MessageBus handles concurrent subscribe/unsubscribe without exceptions")]
    public async Task MessageBus_ConcurrentSubscribeUnsubscribe_NoExceptions()
    {
        // Arrange
        var bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        var consumers = Enumerable.Range(0, 50)
            .Select(_ => new CountingConsumer(() => { }))
            .ToList();

        // Act - subscribe all, then unsubscribe all while publishing
        var subscribeTasks = consumers.Select(c => Task.Run(() => bus.Subscribe(c)));
        await Task.WhenAll(subscribeTasks);

        var publishTasks = Enumerable.Range(0, 50)
            .Select(i => bus.PublishAsync(new ConcurrentTestMessage(i)));
        var unsubscribeTasks = consumers.Select(c => Task.Run(() => bus.Unsubscribe(c)));

        // Should complete without throwing
        await Task.WhenAll(publishTasks.Concat(unsubscribeTasks));
    }

    [Fact(DisplayName = "MessageBus handles concurrent consumer type registration safely")]
    public async Task MessageBus_ConcurrentConsumerTypeRegistration_NoDuplicateExceptions()
    {
        // Arrange
        var bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        // Act - try to register same consumer type concurrently (should be idempotent)
        var registrationTasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => bus.RegisterConsumerType<ConcurrentTestMessage, CountingConsumer>()));

        // Should complete without throwing
        await Task.WhenAll(registrationTasks);
    }

    #endregion

    #region PluginState Concurrent Tests

    [Fact(DisplayName = "PluginState handles concurrent enable/disable safely")]
    public async Task PluginState_ConcurrentEnableDisable_ConsistentState()
    {
        // Arrange
        var pluginState = new PluginState();

        var plugin = CreateTestPluginInfo("test-plugin");
        await pluginState.RegisterPluginAsync(plugin);

        // Act - rapidly toggle enable/disable from multiple threads
        var toggleTasks = Enumerable.Range(0, 50)
            .Select(i => i % 2 == 0
                ? pluginState.EnablePluginAsync("test-plugin")
                : pluginState.DisablePluginAsync("test-plugin"))
            .ToArray();

        await Task.WhenAll(toggleTasks);

        // Assert - plugin should still exist and be in a valid state
        var retrievedPlugin = pluginState.GetPlugin("test-plugin");
        retrievedPlugin.ShouldNotBeNull();
    }

    [Fact(DisplayName = "PluginState handles concurrent registration attempts safely")]
    public async Task PluginState_ConcurrentRegistration_OnlyOneSucceeds()
    {
        // Arrange
        var pluginState = new PluginState();

        // Act - try to register the same plugin ID concurrently
        var registrationTasks = Enumerable.Range(0, 10)
            .Select(i =>
            {
                var plugin = CreateTestPluginInfo("concurrent-plugin");
                return pluginState.RegisterPluginAsync(plugin)
                    .ContinueWith(t => t.IsCompletedSuccessfully ? 1 : 0);
            })
            .ToArray();

        var results = await Task.WhenAll(registrationTasks);

        // Assert - exactly one registration should succeed, others should fail
        results.Sum().ShouldBe(1);
    }

    [Fact(DisplayName = "PluginState handles concurrent Plugins access safely")]
    public async Task PluginState_ConcurrentPluginsAccess_NoExceptions()
    {
        // Arrange
        var pluginState = new PluginState();

        // Register some plugins
        for (int i = 0; i < 10; i++)
        {
            await pluginState.RegisterPluginAsync(CreateTestPluginInfo($"plugin-{i}"));
        }

        // Act - concurrent reads while modifying
        var readTasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => pluginState.Plugins));
        var modifyTasks = Enumerable.Range(10, 10)
            .Select(i => pluginState.RegisterPluginAsync(CreateTestPluginInfo($"plugin-{i}")));

        // Should complete without throwing
        await Task.WhenAll(readTasks.Concat(modifyTasks));
    }

    #endregion

    #region SettingsProvider Concurrent Tests

    [Fact(DisplayName = "SettingsProvider handles concurrent registration safely")]
    public async Task SettingsProvider_ConcurrentRegistration_NoDuplicates()
    {
        // Arrange
        var bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        Services.AddSingleton<IMessageBus>(bus);
        Services.Configure<SettingsRegistrationOptions>(_ => { });

        var provider = new SettingsProvider(
            bus,
            Substitute.For<ILogger<SettingsProvider>>(),
            Services.BuildServiceProvider(),
            Microsoft.Extensions.Options.Options.Create(new SettingsRegistrationOptions()));

        // Act - try to register multiple settings concurrently
        var registrationTasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(() => provider.RegisterSettings(new TestSettings($"settings-{i}"))));

        await Task.WhenAll(registrationTasks);

        // Assert - all 10 settings should be registered
        provider.GetAllSettings().Count.ShouldBe(10);
    }

    [Fact(DisplayName = "SettingsProvider handles concurrent GetAllSettings safely")]
    public async Task SettingsProvider_ConcurrentGetAllSettings_NoExceptions()
    {
        // Arrange
        var bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        var provider = new SettingsProvider(
            bus,
            Substitute.For<ILogger<SettingsProvider>>(),
            Services.BuildServiceProvider(),
            Microsoft.Extensions.Options.Options.Create(new SettingsRegistrationOptions()));

        // Register some settings
        for (int i = 0; i < 5; i++)
        {
            provider.RegisterSettings(new TestSettings($"settings-{i}"));
        }

        // Act - concurrent reads while registering more
        var readTasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => provider.GetAllSettings()));
        var registerTasks = Enumerable.Range(5, 5)
            .Select(i => Task.Run(() => provider.RegisterSettings(new TestSettings($"settings-{i}"))));

        // Should complete without throwing
        await Task.WhenAll(readTasks.Concat(registerTasks));
    }

    [Fact(DisplayName = "SettingsProvider handles concurrent SearchSettings safely")]
    public async Task SettingsProvider_ConcurrentSearchSettings_NoExceptions()
    {
        // Arrange
        var bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        var provider = new SettingsProvider(
            bus,
            Substitute.For<ILogger<SettingsProvider>>(),
            Services.BuildServiceProvider(),
            Microsoft.Extensions.Options.Options.Create(new SettingsRegistrationOptions()));

        // Register some settings
        for (int i = 0; i < 5; i++)
        {
            provider.RegisterSettings(new TestSettings($"settings-{i}"));
        }

        // Act - concurrent searches
        var searchTasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => provider.SearchSettings($"settings-{i % 5}").ToList()));

        // Should complete without throwing
        await Task.WhenAll(searchTasks);
    }

    #endregion

    #region Helper Methods

    private static PluginInfo CreateTestPluginInfo(string id)
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns(id);
        manifest.Name.Returns($"Test Plugin {id}");
        manifest.Version.Returns(new Version(1, 0, 0));

        return new PluginInfo
        {
            Assembly = Assembly.GetExecutingAssembly(),
            Manifest = manifest
        };
    }

    #endregion

    #region Test Types

    private record ConcurrentTestMessage(int Value);

    private class CountingConsumer : IConsumer<ConcurrentTestMessage>
    {
        private readonly Action _onConsume;

        public CountingConsumer(Action onConsume)
        {
            _onConsume = onConsume;
        }

        public CountingConsumer() : this(() => { }) { }

        public Task Consume(ConcurrentTestMessage message)
        {
            _onConsume();
            return Task.CompletedTask;
        }
    }

    private class TestSettings : SettingsBase
    {
        private readonly string _id;

        public TestSettings(string id)
        {
            _id = id;
        }

        public override string SettingsId => _id;
        public override string DisplayName => $"Test Settings {_id}";
        public override string Icon => "settings";

        [Setting(Label = "Test Value")]
        public string TestValue { get; set; } = "default";
    }

    #endregion
}
