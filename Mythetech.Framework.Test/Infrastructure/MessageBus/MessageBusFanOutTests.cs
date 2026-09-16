using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Telemetry;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.MessageBus;

public class MessageBusFanOutTests
{
    private readonly InMemoryMessageBus _bus = new(
        new ServiceCollection().BuildServiceProvider(),
        Substitute.For<ILogger<InMemoryMessageBus>>(),
        [],
        []);

    [Theory(DisplayName = "PublishAsync waits for every consumer that suspends")]
    [InlineData(1)]
    [InlineData(3)]
    public async Task PublishAsync_WaitsForSuspendedConsumers(int suspendingConsumers)
    {
        var gates = Enumerable.Range(0, suspendingConsumers)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();
        var synchronous = new CountingConsumer();
        _bus.Subscribe<FanOutMessage>(synchronous);
        foreach (var gate in gates)
        {
            _bus.Subscribe<FanOutMessage>(new GatedConsumer(gate.Task));
        }

        var publish = _bus.PublishAsync(new FanOutMessage());

        synchronous.Count.ShouldBe(1);
        publish.IsCompleted.ShouldBeFalse();

        foreach (var gate in gates)
        {
            gate.SetResult();
        }
        await publish.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "A consumer that throws before returning a task does not stop the others")]
    public async Task PublishAsync_SynchronousThrow_DoesNotAffectOthers()
    {
        var before = new CountingConsumer();
        var after = new CountingConsumer();
        _bus.Subscribe<FanOutMessage>(before);
        _bus.Subscribe<FanOutMessage>(new SynchronouslyThrowingConsumer());
        _bus.Subscribe<FanOutMessage>(after);

        await _bus.PublishAsync(new FanOutMessage());

        before.Count.ShouldBe(1);
        after.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Unsubscribe removes one subscription of a consumer subscribed twice")]
    public async Task Unsubscribe_RemovesOneOfDuplicateSubscriptions()
    {
        var consumer = new CountingConsumer();
        _bus.Subscribe<FanOutMessage>(consumer);
        _bus.Subscribe<FanOutMessage>(consumer);

        _bus.Unsubscribe<FanOutMessage>(consumer);
        await _bus.PublishAsync(new FanOutMessage());
        consumer.Count.ShouldBe(1);

        _bus.Unsubscribe<FanOutMessage>(consumer);
        await _bus.PublishAsync(new FanOutMessage());
        consumer.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Unsubscribing a consumer that was never subscribed is a no-op")]
    public async Task Unsubscribe_UnknownConsumer_IsNoOp()
    {
        var subscribed = new CountingConsumer();
        _bus.Subscribe<FanOutMessage>(subscribed);

        _bus.Unsubscribe<FanOutMessage>(new CountingConsumer());
        await _bus.PublishAsync(new FanOutMessage());

        subscribed.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "A subscription racing the last unsubscribe is never lost")]
    public void Subscribe_RacingLastUnsubscribe_IsNotLost()
    {
        const int iterations = 20_000;
        var lost = 0;
        using var barrier = new Barrier(2);

        // Each iteration releases both threads together, so the subscribe lands while the only
        // other subscriber of the message type is being removed.
        var unsubscriber = new Thread(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                var leaving = new CountingConsumer();
                _bus.Subscribe<FanOutMessage>(leaving);
                barrier.SignalAndWait();
                _bus.Unsubscribe<FanOutMessage>(leaving);
                barrier.SignalAndWait();
            }
        });
        var subscriber = new Thread(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                var joining = new CountingConsumer();
                barrier.SignalAndWait();
                _bus.Subscribe<FanOutMessage>(joining);
                barrier.SignalAndWait();

                // Synchronous consumers only, so the publish has completed when it returns.
                _bus.PublishAsync(new FanOutMessage()).IsCompletedSuccessfully.ShouldBeTrue();
                if (joining.Count == 0)
                    lost++;
                _bus.Unsubscribe<FanOutMessage>(joining);
            }
        });

        unsubscriber.Start();
        subscriber.Start();
        unsubscriber.Join();
        subscriber.Join();

        lost.ShouldBe(0);
    }

    [Fact(DisplayName = "With a listener attached, consume activities are children of the publish activity")]
    public async Task PublishAsync_WithListener_RecordsNestedActivities()
    {
        var stopped = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FrameworkTelemetry.MessageBusSource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);

        _bus.Subscribe<TelemetryMessage>(new CountingConsumer());
        _bus.Subscribe<TelemetryMessage>(new GatedConsumer(Task.Delay(10)));

        await _bus.PublishAsync(new TelemetryMessage());

        // The listener is process-wide, so other tests' activities are filtered out by message type.
        var ours = stopped
            .Where(a => (string?)a.GetTagItem(FrameworkTelemetry.Tags.MessageType) == nameof(TelemetryMessage))
            .ToList();
        var publish = ours.Where(a => a.OperationName == $"Publish:{nameof(TelemetryMessage)}").ShouldHaveSingleItem();
        publish.GetTagItem(FrameworkTelemetry.Tags.ConsumerCount).ShouldBe(2);
        publish.GetTagItem(FrameworkTelemetry.Tags.Success).ShouldBe(true);

        var consumes = ours.Where(a => a.OperationName.StartsWith("Consume:")).ToList();
        consumes.Select(a => a.OperationName).ShouldBe(
            [$"Consume:{nameof(CountingConsumer)}", $"Consume:{nameof(GatedConsumer)}"],
            ignoreOrder: true);
        consumes.ShouldAllBe(a => a.ParentId == publish.Id);
    }

    private sealed record FanOutMessage;

    private sealed record TelemetryMessage;

    private sealed class CountingConsumer : IConsumer<FanOutMessage>, IConsumer<TelemetryMessage>
    {
        private int _count;

        public int Count => _count;

        public Task Consume(FanOutMessage message)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }

        public Task Consume(TelemetryMessage message) => Consume(new FanOutMessage());
    }

    private sealed class GatedConsumer(Task gate) : IConsumer<FanOutMessage>, IConsumer<TelemetryMessage>
    {
        public async Task Consume(FanOutMessage message) => await gate;

        public async Task Consume(TelemetryMessage message) => await gate;
    }

    private sealed class SynchronouslyThrowingConsumer : IConsumer<FanOutMessage>
    {
        public Task Consume(FanOutMessage message) => throw new InvalidOperationException("Thrown before a task exists");
    }
}
