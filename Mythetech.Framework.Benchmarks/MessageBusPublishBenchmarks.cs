using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Benchmarks;

/// <summary>
/// Measures <see cref="InMemoryMessageBus.PublishAsync{TMessage}(TMessage)"/> across consumer
/// shapes and fan-out.
/// </summary>
/// <remarks>
/// Consumers that complete synchronously (<see cref="ConsumerShape.Synchronous"/>, as many real
/// consumers do) measure the bus's own overhead. Consumers that yield
/// (<see cref="ConsumerShape.Yielding"/>) add a real suspension and thread pool hop per consumer,
/// which is what the fan-out axis compounds.
/// <para>
/// <see cref="DirectFanOut"/> awaits the same consumers without pipes, filters, cancellation or
/// telemetry, so the gap between the two is the bus's own cost.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class MessageBusPublishBenchmarks
{
    private readonly BenchmarkMessage _message = new();
    private InMemoryMessageBus _bus = null!;
    private IConsumer<BenchmarkMessage>[] _consumers = [];

    [Params(1, 4, 16)]
    public int ConsumerCount { get; set; }

    [Params(ConsumerShape.Synchronous, ConsumerShape.Yielding)]
    public ConsumerShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bus = new InMemoryMessageBus(
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<InMemoryMessageBus>.Instance,
            [],
            []);

        _consumers = Enumerable.Range(0, ConsumerCount)
            .Select(IConsumer<BenchmarkMessage> (_) => Shape == ConsumerShape.Synchronous
                ? new SynchronousConsumer()
                : new YieldingConsumer())
            .ToArray();

        foreach (var consumer in _consumers)
        {
            _bus.Subscribe(consumer);
        }
    }

    [Benchmark(Baseline = true)]
    public Task Publish() => _bus.PublishAsync(_message);

    [Benchmark]
    public Task DirectFanOut() => Task.WhenAll(_consumers.Select(async consumer => await consumer.Consume(_message)));
}

public enum ConsumerShape
{
    Synchronous,
    Yielding,
}

public sealed class BenchmarkMessage;

public sealed class SynchronousConsumer : IConsumer<BenchmarkMessage>
{
    public Task Consume(BenchmarkMessage message) => Task.CompletedTask;
}

public sealed class YieldingConsumer : IConsumer<BenchmarkMessage>
{
    public async Task Consume(BenchmarkMessage message) => await Task.Yield();
}
