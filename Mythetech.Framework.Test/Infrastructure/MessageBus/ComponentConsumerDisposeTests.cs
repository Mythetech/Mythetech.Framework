using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.MessageBus;

public class ComponentConsumerDisposeTests : BunitContext
{
    private readonly InMemoryMessageBus _bus;

    public ComponentConsumerDisposeTests()
    {
        _bus = new InMemoryMessageBus(Services, Substitute.For<ILogger<InMemoryMessageBus>>(), [], []);
        Services.AddSingleton<IMessageBus>(_bus);
    }

    [Fact(DisplayName = "DisposeAsync runs a ComponentConsumer's DisposeAsyncCore and still unsubscribes")]
    public async Task DisposeAsync_RunsDisposeAsyncCore_AndUnsubscribes()
    {
        var consumer = Render<DisposeHookConsumer>().Instance;

        await consumer.DisposeAsync();
        await _bus.PublishAsync(new TestCommand("After"));

        consumer.DisposeAsyncCoreCalls.ShouldBe(1);
        consumer.Received.ShouldBe(0);
    }

    [Fact(DisplayName = "DisposeAsync runs a multi-message ComponentConsumer's DisposeAsyncCore and still unsubscribes")]
    public async Task MultiMessage_DisposeAsync_RunsDisposeAsyncCore_AndUnsubscribes()
    {
        var consumer = Render<DualDisposeHookConsumer>().Instance;

        await consumer.DisposeAsync();
        await _bus.PublishAsync(new TestCommand("After"));
        await _bus.PublishAsync(new TestCommand2(1));

        consumer.DisposeAsyncCoreCalls.ShouldBe(1);
        consumer.Received.ShouldBe(0);
    }
}

public class DisposeHookConsumer : ComponentConsumer<TestCommand>
{
    public int Received { get; private set; }
    public int DisposeAsyncCoreCalls { get; private set; }

    protected override Task Consume(TestCommand message, CancellationToken cancellationToken)
    {
        Received++;
        return Task.CompletedTask;
    }

    protected override ValueTask DisposeAsyncCore()
    {
        DisposeAsyncCoreCalls++;
        return ValueTask.CompletedTask;
    }
}

public class DualDisposeHookConsumer : ComponentConsumer<TestCommand, TestCommand2>
{
    public int Received { get; private set; }
    public int DisposeAsyncCoreCalls { get; private set; }

    protected override Task Consume(TestCommand message, CancellationToken cancellationToken)
    {
        Received++;
        return Task.CompletedTask;
    }

    protected override Task Consume(TestCommand2 message, CancellationToken cancellationToken)
    {
        Received++;
        return Task.CompletedTask;
    }

    protected override ValueTask DisposeAsyncCore()
    {
        DisposeAsyncCoreCalls++;
        return ValueTask.CompletedTask;
    }
}
