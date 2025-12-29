using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.MessageBus;

public class MessageBusTests : TestContext
{
    private IMessageBus _bus;
    
    public MessageBusTests()
    {
        Services.AddSingleton<TestConsumer>();
        _bus = new InMemoryMessageBus(
            this.Services, 
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());
        Services.AddSingleton<IMessageBus>(_bus);
    }

    [Fact(DisplayName = "Can send messages to be received by services")]
    public async Task Can_Message_Services()
    {
        // Arrange
        _bus.RegisterConsumerType<TestCommand, TestConsumer>();
        
        // Act
        await _bus.PublishAsync(new TestCommand("HelloWorld!"));
        
        // Assert
        var consumer = Services.GetRequiredService<TestConsumer>();
        consumer.Text.ShouldBe("HelloWorld!");
    }
    
    [Fact(DisplayName = "Can send messages to be received by component consumers")]
    public async Task Can_Message_Components()
    {
        // Arrange
        _bus.RegisterConsumerType<TestCommand, TestComponentConsumer>();
        var cut = RenderComponent<TestComponentConsumer>();
        
        // Act
        await _bus.PublishAsync(new TestCommand("HelloWorld!"));
        
        // Assert
        cut.Markup.MarkupMatches("<p>HelloWorld!</p>");
    }

    [Fact(DisplayName = "When a consumer throws an exception, other consumers still receive the message")]
    public async Task Exception_In_One_Consumer_Does_Not_Affect_Others()
    {
        // Arrange
        _bus.RegisterConsumerType<TestCommand, TestComponentConsumer>();
        _bus.RegisterConsumerType<TestCommand, TestExceptionThrowingConsumer>();
        
        var normalConsumer = RenderComponent<TestComponentConsumer>();
        var throwingConsumer = RenderComponent<TestExceptionThrowingConsumer>();
        
        // Act 
        await _bus.PublishAsync(new TestCommand("HelloWorld!"));
        
        // Assert
        normalConsumer.Markup.MarkupMatches("<p>HelloWorld!</p>");
    }
    
    [Fact(DisplayName = "When multiple consumers throw an exception, other consumers still receive the message")]
    public async Task Exceptions_In_Multiple_Consumers_Does_Not_Affect_Others()
    {
        // Arrange
        _bus.RegisterConsumerType<TestCommand, TestComponentConsumer>();
        _bus.RegisterConsumerType<TestCommand, TestExceptionThrowingConsumer>();
        
        var initialThrowingConsumer = RenderComponent<TestExceptionThrowingConsumer>();
        var throwingConsumer = RenderComponent<TestExceptionThrowingConsumer>();
        var normalConsumer = RenderComponent<TestComponentConsumer>();
        
        // Act 
        await _bus.PublishAsync(new TestCommand("HelloWorld!"));
        
        // Assert
        normalConsumer.Markup.MarkupMatches("<p>HelloWorld!</p>");
    }

    [Fact(DisplayName = "PublishAsync with timeout completes before slow consumers finish")]
    public async Task PublishAsync_WithTimeout_CompletesBeforeSlowConsumers()
    {
        // Arrange
        var slowConsumer = new SlowConsumer(TimeSpan.FromSeconds(10));
        _bus.Subscribe(slowConsumer);
        
        var config = new PublishConfiguration { Timeout = TimeSpan.FromMilliseconds(100) };
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _bus.PublishAsync(new TestCommand("Test"), config);
        stopwatch.Stop();
        
        // Assert
        slowConsumer.ReceivedMessage.ShouldBeTrue();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000);
        slowConsumer.CompletedWithoutCancellation.ShouldBeFalse();
    }
    
    [Fact(DisplayName = "PublishAsync with cancellation token respects external cancellation")]
    public async Task PublishAsync_WithCancellationToken_RespectsExternalCancellation()
    {
        // Arrange
        var slowConsumer = new SlowConsumer(TimeSpan.FromSeconds(10));
        _bus.Subscribe(slowConsumer);
        
        using var cts = new CancellationTokenSource();
        var config = new PublishConfiguration { CancellationToken = cts.Token };
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var publishTask = _bus.PublishAsync(new TestCommand("Test"), config);
        await Task.Delay(50);
        cts.Cancel();
        await publishTask;
        stopwatch.Stop();
        
        // Assert
        slowConsumer.ReceivedMessage.ShouldBeTrue();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000);
    }
    
    [Fact(DisplayName = "PublishAsync without configuration still works with no timeout")]
    public async Task PublishAsync_DefaultOverload_WorksWithoutTimeout()
    {
        // Arrange
        var fastConsumer = new SlowConsumer(TimeSpan.FromMilliseconds(10));
        _bus.Subscribe(fastConsumer);
        
        // Act
        await _bus.PublishAsync(new TestCommand("Test"));
        
        // Assert
        fastConsumer.ReceivedMessage.ShouldBeTrue();
        fastConsumer.CompletedWithoutCancellation.ShouldBeTrue();
    }
    
    [Fact(DisplayName = "PublishAsync with timeout still delivers to fast consumers")]
    public async Task PublishAsync_WithTimeout_DeliversToFastConsumers()
    {
        // Arrange
        var fastConsumer = new SlowConsumer(TimeSpan.FromMilliseconds(5));
        var slowConsumer = new SlowConsumer(TimeSpan.FromSeconds(10));
        _bus.Subscribe(fastConsumer);
        _bus.Subscribe(slowConsumer);
        
        var config = new PublishConfiguration { Timeout = TimeSpan.FromMilliseconds(100) };
        
        // Act
        await _bus.PublishAsync(new TestCommand("Test"), config);
        
        // Assert
        fastConsumer.ReceivedMessage.ShouldBeTrue();
        fastConsumer.CompletedWithoutCancellation.ShouldBeTrue();
        slowConsumer.ReceivedMessage.ShouldBeTrue();
        slowConsumer.CompletedWithoutCancellation.ShouldBeFalse();
    }
}

public record TestCommand(string Text);

public class TestConsumer : IConsumer<TestCommand>
{
    public string? Text { get; set; }
    
    public Task Consume(TestCommand message)
    {
        Text = message.Text;
        return Task.CompletedTask;
    }
}

public class SlowConsumer : IConsumer<TestCommand>
{
    private readonly TimeSpan _delay;
    
    public bool ReceivedMessage { get; private set; }
    public bool CompletedWithoutCancellation { get; private set; }
    
    public SlowConsumer(TimeSpan delay)
    {
        _delay = delay;
    }
    
    public async Task Consume(TestCommand message)
    {
        ReceivedMessage = true;
        await Task.Delay(_delay);
        CompletedWithoutCancellation = true;
    }
}