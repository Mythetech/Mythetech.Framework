using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.MessageBus;

public class MessageBusPipelineTests : TestContext
{
    private readonly ILogger<InMemoryMessageBus> _logger = Substitute.For<ILogger<InMemoryMessageBus>>();

    [Fact(DisplayName = "Global pipe that returns false blocks message from reaching consumers")]
    public async Task GlobalPipe_ReturnsFalse_BlocksConsumers()
    {
        // Arrange
        var blockingPipe = new BlockingPipe();
        var consumer = new TrackingConsumer();
        
        var bus = new InMemoryMessageBus(
            Services,
            _logger,
            [blockingPipe],
            []);
        
        bus.Subscribe(consumer);
        
        // Act
        await bus.PublishAsync(new PipelineTestMessage("Test"));
        
        // Assert
        consumer.ReceivedCount.ShouldBe(0);
        blockingPipe.ProcessedCount.ShouldBe(1);
    }
    
    [Fact(DisplayName = "Global pipe that returns true allows message to reach consumers")]
    public async Task GlobalPipe_ReturnsTrue_AllowsConsumers()
    {
        // Arrange
        var passingPipe = new PassingPipe();
        var consumer = new TrackingConsumer();
        
        var bus = new InMemoryMessageBus(
            Services,
            _logger,
            [passingPipe],
            []);
        
        bus.Subscribe(consumer);
        
        // Act
        await bus.PublishAsync(new PipelineTestMessage("Test"));
        
        // Assert
        consumer.ReceivedCount.ShouldBe(1);
        passingPipe.ProcessedCount.ShouldBe(1);
    }
    
    [Fact(DisplayName = "Multiple global pipes run in order, first blocking stops the chain")]
    public async Task MultiplePipes_FirstBlockingStopsChain()
    {
        // Arrange
        var firstPipe = new PassingPipe();
        var blockingPipe = new BlockingPipe();
        var thirdPipe = new PassingPipe();
        var consumer = new TrackingConsumer();
        
        var bus = new InMemoryMessageBus(
            Services,
            _logger,
            [firstPipe, blockingPipe, thirdPipe],
            []);
        
        bus.Subscribe(consumer);
        
        // Act
        await bus.PublishAsync(new PipelineTestMessage("Test"));
        
        // Assert
        firstPipe.ProcessedCount.ShouldBe(1);
        blockingPipe.ProcessedCount.ShouldBe(1);
        thirdPipe.ProcessedCount.ShouldBe(0);
        consumer.ReceivedCount.ShouldBe(0);
    }
    
    [Fact(DisplayName = "Consumer filter that returns false skips specific consumer")]
    public async Task ConsumerFilter_ReturnsFalse_SkipsConsumer()
    {
        // Arrange
        var allowedConsumer = new TrackingConsumer();
        var blockedConsumer = new TrackingConsumer();
        var filter = new SelectiveFilter(blockedConsumer);
        
        var bus = new InMemoryMessageBus(
            Services,
            _logger,
            [],
            [filter]);
        
        bus.Subscribe(allowedConsumer);
        bus.Subscribe(blockedConsumer);
        
        // Act
        await bus.PublishAsync(new PipelineTestMessage("Test"));
        
        // Assert
        allowedConsumer.ReceivedCount.ShouldBe(1);
        blockedConsumer.ReceivedCount.ShouldBe(0);
    }
    
    [Fact(DisplayName = "Multiple filters must all return true for consumer to receive message")]
    public async Task MultipleFilters_AllMustPass()
    {
        // Arrange
        var consumer = new TrackingConsumer();
        var passingFilter = new AlwaysPassFilter();
        var blockingFilter = new AlwaysBlockFilter();
        
        var bus = new InMemoryMessageBus(
            Services,
            _logger,
            [],
            [passingFilter, blockingFilter]);
        
        bus.Subscribe(consumer);
        
        // Act
        await bus.PublishAsync(new PipelineTestMessage("Test"));
        
        // Assert
        consumer.ReceivedCount.ShouldBe(0);
    }
    
    [Fact(DisplayName = "Typed pipe only runs for matching message types")]
    public async Task TypedPipe_OnlyRunsForMatchingType()
    {
        // Arrange
        var typedPipe = new TypedBlockingPipe();
        Services.AddSingleton<IMessagePipe<OtherMessage>>(typedPipe);
        
        var consumer = new TrackingConsumer();
        
        var bus = new InMemoryMessageBus(
            Services,
            _logger,
            [],
            []);
        
        bus.Subscribe(consumer);
        
        // Act - send PipelineTestMessage, typed pipe is for OtherMessage
        await bus.PublishAsync(new PipelineTestMessage("Test"));
        
        // Assert - consumer should receive because typed pipe doesn't match
        consumer.ReceivedCount.ShouldBe(1);
        typedPipe.ProcessedCount.ShouldBe(0);
    }
    
    [Fact(DisplayName = "Typed pipe blocks matching message type")]
    public async Task TypedPipe_BlocksMatchingType()
    {
        // Arrange
        var typedPipe = new TypedBlockingPipeForTestMessage();
        Services.AddSingleton<IMessagePipe<PipelineTestMessage>>(typedPipe);
        
        var consumer = new TrackingConsumer();
        
        var bus = new InMemoryMessageBus(
            Services,
            _logger,
            [],
            []);
        
        bus.Subscribe(consumer);
        
        // Act
        await bus.PublishAsync(new PipelineTestMessage("Test"));
        
        // Assert
        consumer.ReceivedCount.ShouldBe(0);
        typedPipe.ProcessedCount.ShouldBe(1);
    }

    [Fact(DisplayName = "Pipe exceptions are caught and don't block other pipes or consumers")]
    public async Task PipeException_DoesNotBlockProcessing()
    {
        // Arrange
        var throwingPipe = new ThrowingPipe();
        var passingPipe = new PassingPipe();
        var consumer = new TrackingConsumer();
        
        var bus = new InMemoryMessageBus(
            Services,
            _logger,
            [throwingPipe, passingPipe],
            []);
        
        bus.Subscribe(consumer);
        
        // Act
        await bus.PublishAsync(new PipelineTestMessage("Test"));
        
        // Assert - pipe threw but processing continued
        passingPipe.ProcessedCount.ShouldBe(1);
        consumer.ReceivedCount.ShouldBe(1);
    }
}

#region Test Types

public record PipelineTestMessage(string Content);
public record OtherMessage(string Content);

public class TrackingConsumer : IConsumer<PipelineTestMessage>
{
    public int ReceivedCount { get; private set; }
    
    public Task Consume(PipelineTestMessage message)
    {
        ReceivedCount++;
        return Task.CompletedTask;
    }
}

public class BlockingPipe : IMessagePipe
{
    public int ProcessedCount { get; private set; }
    
    public Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken) 
        where TMessage : class
    {
        ProcessedCount++;
        return Task.FromResult(false);
    }
}

public class PassingPipe : IMessagePipe
{
    public int ProcessedCount { get; private set; }
    
    public Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken) 
        where TMessage : class
    {
        ProcessedCount++;
        return Task.FromResult(true);
    }
}

public class ThrowingPipe : IMessagePipe
{
    public Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken) 
        where TMessage : class
    {
        throw new InvalidOperationException("Pipe error");
    }
}

public class TypedBlockingPipe : IMessagePipe<OtherMessage>
{
    public int ProcessedCount { get; private set; }
    
    public Task<bool> ProcessAsync(OtherMessage message, CancellationToken cancellationToken)
    {
        ProcessedCount++;
        return Task.FromResult(false);
    }
}

public class TypedBlockingPipeForTestMessage : IMessagePipe<PipelineTestMessage>
{
    public int ProcessedCount { get; private set; }
    
    public Task<bool> ProcessAsync(PipelineTestMessage message, CancellationToken cancellationToken)
    {
        ProcessedCount++;
        return Task.FromResult(false);
    }
}

public class SelectiveFilter : IConsumerFilter
{
    private readonly object _blockedConsumer;
    
    public SelectiveFilter(object blockedConsumer)
    {
        _blockedConsumer = blockedConsumer;
    }
    
    public bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message) 
        where TMessage : class
    {
        return !ReferenceEquals(consumer, _blockedConsumer);
    }
}

public class AlwaysPassFilter : IConsumerFilter
{
    public bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message) 
        where TMessage : class => true;
}

public class AlwaysBlockFilter : IConsumerFilter
{
    public bool ShouldInvoke<TMessage>(IConsumer<TMessage> consumer, TMessage message) 
        where TMessage : class => false;
}

#endregion

