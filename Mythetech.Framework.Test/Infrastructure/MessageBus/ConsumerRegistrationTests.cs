using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Test.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.MessageBus;

public class ConsumerRegistrationTests
{
    private readonly ServiceCollection _services = new();

    private InMemoryMessageBus CreateBus() => new(
        _services.BuildServiceProvider(),
        Substitute.For<ILogger<InMemoryMessageBus>>(),
        [],
        []);

    [Fact(DisplayName = "Registering the same consumer type twice invokes it once per publish")]
    public async Task Same_consumer_registered_twice_runs_once()
    {
        var consumer = new CountingRegistrationConsumer();
        _services.AddSingleton(consumer);
        var bus = CreateBus();

        bus.RegisterConsumerType<RegistrationCountMessage, CountingRegistrationConsumer>();
        bus.RegisterConsumerType<RegistrationCountMessage, CountingRegistrationConsumer>();
        await bus.PublishAsync(new RegistrationCountMessage());

        consumer.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Registering the same assembly twice invokes each consumer once per publish")]
    public async Task Same_assembly_registered_twice_runs_each_consumer_once()
    {
        var consumer = new CountingRegistrationConsumer();
        _services.AddSingleton(consumer);
        var bus = CreateBus();
        var assembly = typeof(ConsumerRegistrationTests).Assembly;

        bus.RegisterConsumersToBus(assembly);
        bus.RegisterConsumersToBus(assembly);
        await bus.PublishAsync(new RegistrationCountMessage());
        await bus.PublishAsync(new RegistrationCountMessage());

        consumer.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Different consumer types for the same message each run once")]
    public async Task Different_consumers_for_one_message_each_run_once()
    {
        var first = new CountingRegistrationConsumer();
        var second = new OtherCountingRegistrationConsumer();
        _services.AddSingleton(first);
        _services.AddSingleton(second);
        var bus = CreateBus();

        bus.RegisterConsumerType<RegistrationCountMessage, CountingRegistrationConsumer>();
        bus.RegisterConsumerType<RegistrationCountMessage, OtherCountingRegistrationConsumer>();
        bus.RegisterConsumerType<RegistrationCountMessage, CountingRegistrationConsumer>();
        await bus.PublishAsync(new RegistrationCountMessage());

        first.Count.ShouldBe(1);
        second.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "A host that registers the framework assembly through both UseMessageBus and UseSettingsFramework saves a change once")]
    public async Task Framework_assembly_registered_twice_saves_settings_once()
    {
        var storage = Substitute.For<ISettingsStorage>();
        _services.AddLogging();
        _services.AddMessageBus();
        _services.AddSettingsFramework();
        _services.AddSingleton(storage);
        var serviceProvider = _services.BuildServiceProvider();
        serviceProvider.UseMessageBus(typeof(IConsumer<>).Assembly);
        serviceProvider.UseSettingsFramework();

        var settings = new TestSettings();
        var provider = serviceProvider.GetRequiredService<ISettingsProvider>();
        provider.RegisterSettings(settings);
        await provider.NotifySettingsChangedAsync(settings);

        await storage.Received(1).SaveSettingsAsync(settings.SettingsId, Arg.Any<string>());
    }
}

public record RegistrationCountMessage;

public class CountingRegistrationConsumer : IConsumer<RegistrationCountMessage>
{
    public int Count { get; private set; }

    public Task Consume(RegistrationCountMessage message)
    {
        Count++;
        return Task.CompletedTask;
    }
}

public class OtherCountingRegistrationConsumer : IConsumer<RegistrationCountMessage>
{
    public int Count { get; private set; }

    public Task Consume(RegistrationCountMessage message)
    {
        Count++;
        return Task.CompletedTask;
    }
}
