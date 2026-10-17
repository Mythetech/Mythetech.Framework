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

    [Fact(DisplayName = "A consumer registered after its message type was first published receives later messages")]
    public async Task Consumer_registered_after_first_publish_receives_later_messages()
    {
        var consumer = new CountingRegistrationConsumer();
        _services.AddSingleton(consumer);
        var bus = CreateBus();

        await bus.PublishAsync(new RegistrationCountMessage());
        bus.RegisterConsumerType<RegistrationCountMessage, CountingRegistrationConsumer>();
        await bus.PublishAsync(new RegistrationCountMessage());

        consumer.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "A consumer whose constructor registers another consumer for its message type runs both")]
    public async Task Consumer_registering_another_while_resolving_runs_both()
    {
        _services.AddSingleton<IMessageBus>(sp => new InMemoryMessageBus(sp, Substitute.For<ILogger<InMemoryMessageBus>>(), [], []));
        _services.AddSingleton<SelfRegisteringConsumer>();
        _services.AddSingleton<CountingRegistrationConsumer>();
        var provider = _services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageBus>();

        bus.RegisterConsumerType<RegistrationCountMessage, SelfRegisteringConsumer>();
        await bus.PublishAsync(new RegistrationCountMessage());

        provider.GetRequiredService<SelfRegisteringConsumer>().Count.ShouldBe(1);
        provider.GetRequiredService<CountingRegistrationConsumer>().Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Registering a consumer type for a message type it doesn't consume throws")]
    public void Registering_consumer_for_other_message_type_throws()
    {
        IMessageBus bus = CreateBus();

        Should.Throw<ArgumentException>(() => bus.RegisterConsumerType(typeof(string), typeof(CountingRegistrationConsumer)));
    }

    [Fact(DisplayName = "Registering a query handler type for a query it doesn't answer throws")]
    public void Registering_query_handler_for_other_query_throws()
    {
        IMessageBus bus = CreateBus();

        Should.Throw<ArgumentException>(() => bus.RegisterQueryHandler(typeof(RegistrationCountMessage), typeof(string), typeof(CountingRegistrationConsumer)));
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

public class SelfRegisteringConsumer : IConsumer<RegistrationCountMessage>
{
    public SelfRegisteringConsumer(IMessageBus bus)
        => bus.RegisterConsumerType<RegistrationCountMessage, CountingRegistrationConsumer>();

    public int Count { get; private set; }

    public Task Consume(RegistrationCountMessage message)
    {
        Count++;
        return Task.CompletedTask;
    }
}
