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

    #region ComponentConsumer<T1, T2> Tests

    [Fact(DisplayName = "ComponentConsumer<T1, T2> receives first message type")]
    public async Task DualConsumer_ReceivesFirstMessageType()
    {
        // Arrange
        var cut = RenderComponent<TestDualConsumer>();

        // Act
        await _bus.PublishAsync(new TestCommand("Hello"));

        // Assert
        cut.Instance.ReceivedText.ShouldBe("Hello");
        cut.Instance.ReceivedValue.ShouldBeNull();
    }

    [Fact(DisplayName = "ComponentConsumer<T1, T2> receives second message type")]
    public async Task DualConsumer_ReceivesSecondMessageType()
    {
        // Arrange
        var cut = RenderComponent<TestDualConsumer>();

        // Act
        await _bus.PublishAsync(new TestCommand2(42));

        // Assert
        cut.Instance.ReceivedText.ShouldBeNull();
        cut.Instance.ReceivedValue.ShouldBe(42);
    }

    [Fact(DisplayName = "ComponentConsumer<T1, T2> receives both message types")]
    public async Task DualConsumer_ReceivesBothMessageTypes()
    {
        // Arrange
        var cut = RenderComponent<TestDualConsumer>();

        // Act
        await _bus.PublishAsync(new TestCommand("Hello"));
        await _bus.PublishAsync(new TestCommand2(42));

        // Assert
        cut.Instance.ReceivedText.ShouldBe("Hello");
        cut.Instance.ReceivedValue.ShouldBe(42);
    }

    [Fact(DisplayName = "ComponentConsumer<T1, T2> unsubscribes on dispose")]
    public async Task DualConsumer_UnsubscribesOnDispose()
    {
        // Arrange
        var cut = RenderComponent<TestDualConsumer>();
        var instance = cut.Instance;

        // Act - dispose the component explicitly
        ((IDisposable)instance).Dispose();

        // Publish messages after dispose
        await _bus.PublishAsync(new TestCommand("After"));
        await _bus.PublishAsync(new TestCommand2(99));

        // Assert - values should not have changed
        instance.ReceivedText.ShouldBeNull();
        instance.ReceivedValue.ShouldBeNull();
    }

    #endregion

    #region ComponentConsumer<T1, T2, T3> Tests

    [Fact(DisplayName = "ComponentConsumer<T1, T2, T3> receives all three message types")]
    public async Task TripleConsumer_ReceivesAllMessageTypes()
    {
        // Arrange
        var cut = RenderComponent<TestTripleConsumer>();

        // Act
        await _bus.PublishAsync(new TestCommand("Hello"));
        await _bus.PublishAsync(new TestCommand2(42));
        await _bus.PublishAsync(new TestCommand3(true));

        // Assert
        cut.Instance.ReceivedText.ShouldBe("Hello");
        cut.Instance.ReceivedValue.ShouldBe(42);
        cut.Instance.ReceivedFlag.ShouldBe(true);
    }

    [Fact(DisplayName = "ComponentConsumer<T1, T2, T3> unsubscribes on dispose")]
    public async Task TripleConsumer_UnsubscribesOnDispose()
    {
        // Arrange
        var cut = RenderComponent<TestTripleConsumer>();
        var instance = cut.Instance;

        // Act - dispose the component explicitly
        ((IDisposable)instance).Dispose();

        // Publish messages after dispose
        await _bus.PublishAsync(new TestCommand("After"));
        await _bus.PublishAsync(new TestCommand2(99));
        await _bus.PublishAsync(new TestCommand3(true));

        // Assert - values should not have changed
        instance.ReceivedText.ShouldBeNull();
        instance.ReceivedValue.ShouldBeNull();
        instance.ReceivedFlag.ShouldBeNull();
    }

    #endregion

    #region ComponentConsumer<T1, T2, T3, T4> Tests

    [Fact(DisplayName = "ComponentConsumer<T1, T2, T3, T4> receives all four message types")]
    public async Task QuadConsumer_ReceivesAllMessageTypes()
    {
        // Arrange
        var cut = RenderComponent<TestQuadConsumer>();

        // Act
        await _bus.PublishAsync(new TestCommand("Hello"));
        await _bus.PublishAsync(new TestCommand2(42));
        await _bus.PublishAsync(new TestCommand3(true));
        await _bus.PublishAsync(new TestCommand4(3.14));

        // Assert
        cut.Instance.ReceivedText.ShouldBe("Hello");
        cut.Instance.ReceivedValue.ShouldBe(42);
        cut.Instance.ReceivedFlag.ShouldBe(true);
        cut.Instance.ReceivedAmount.ShouldBe(3.14);
    }

    [Fact(DisplayName = "ComponentConsumer<T1, T2, T3, T4> unsubscribes on dispose")]
    public async Task QuadConsumer_UnsubscribesOnDispose()
    {
        // Arrange
        var cut = RenderComponent<TestQuadConsumer>();
        var instance = cut.Instance;

        // Act - dispose the component explicitly
        ((IDisposable)instance).Dispose();

        // Publish messages after dispose
        await _bus.PublishAsync(new TestCommand("After"));
        await _bus.PublishAsync(new TestCommand2(99));
        await _bus.PublishAsync(new TestCommand3(true));
        await _bus.PublishAsync(new TestCommand4(9.99));

        // Assert - values should not have changed
        instance.ReceivedText.ShouldBeNull();
        instance.ReceivedValue.ShouldBeNull();
        instance.ReceivedFlag.ShouldBeNull();
        instance.ReceivedAmount.ShouldBeNull();
    }

    #endregion
}

public record TestCommand(string Text);
public record TestCommand2(int Value);
public record TestCommand3(bool Flag);
public record TestCommand4(double Amount);

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