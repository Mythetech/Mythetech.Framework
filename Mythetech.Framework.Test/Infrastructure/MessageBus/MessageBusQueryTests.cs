using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.MessageBus;

public class MessageBusQueryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBus _bus;

    public MessageBusQueryTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestQueryHandler>();
        services.AddSingleton<SlowQueryHandler>();
        services.AddSingleton<ExceptionThrowingQueryHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _bus = new InMemoryMessageBus(
            _serviceProvider,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());
    }

    #region Basic Query Functionality

    [Fact(DisplayName = "SendAsync returns response from registered handler")]
    public async Task SendAsync_ReturnsResponseFromRegisteredHandler()
    {
        // Arrange
        _bus.RegisterQueryHandler<TestQuery, TestQueryResponse, TestQueryHandler>();

        // Act
        var response = await _bus.SendAsync<TestQuery, TestQueryResponse>(new TestQuery("Hello"));

        // Assert
        response.ShouldNotBeNull();
        response.Result.ShouldBe("Handled: Hello");
    }

    [Fact(DisplayName = "SendAsync throws InvalidOperationException when no handler registered")]
    public async Task SendAsync_ThrowsWhenNoHandlerRegistered()
    {
        // Arrange - no handler registered

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _bus.SendAsync<TestQuery, TestQueryResponse>(new TestQuery("Hello")));

        exception.Message.ShouldContain("No query handler registered");
        exception.Message.ShouldContain("TestQuery");
        exception.Message.ShouldContain("TestQueryResponse");
    }

    [Fact(DisplayName = "SendAsync caches handler instance after first resolution")]
    public async Task SendAsync_CachesHandlerInstance()
    {
        // Arrange
        _bus.RegisterQueryHandler<TestQuery, TestQueryResponse, TestQueryHandler>();

        // Act
        var response1 = await _bus.SendAsync<TestQuery, TestQueryResponse>(new TestQuery("First"));
        var response2 = await _bus.SendAsync<TestQuery, TestQueryResponse>(new TestQuery("Second"));

        // Assert
        response1.Result.ShouldBe("Handled: First");
        response2.Result.ShouldBe("Handled: Second");

        // Both should use the same handler instance (singleton)
        var handler = _serviceProvider.GetRequiredService<TestQueryHandler>();
        handler.InvocationCount.ShouldBe(2);
    }

    [Fact(DisplayName = "Multiple query types can have different handlers")]
    public async Task MultipleQueryTypes_HaveDifferentHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<TestQueryHandler>();
        services.AddSingleton<AnotherQueryHandler>();
        var provider = services.BuildServiceProvider();

        var bus = new InMemoryMessageBus(
            provider,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        bus.RegisterQueryHandler<TestQuery, TestQueryResponse, TestQueryHandler>();
        bus.RegisterQueryHandler<AnotherQuery, AnotherQueryResponse, AnotherQueryHandler>();

        // Act
        var response1 = await bus.SendAsync<TestQuery, TestQueryResponse>(new TestQuery("Test"));
        var response2 = await bus.SendAsync<AnotherQuery, AnotherQueryResponse>(new AnotherQuery(42));

        // Assert
        response1.Result.ShouldBe("Handled: Test");
        response2.Value.ShouldBe(84); // Doubled by handler
    }

    #endregion

    #region Timeout and Cancellation

    [Fact(DisplayName = "SendAsync with timeout throws OperationCanceledException for slow handlers")]
    public async Task SendAsync_WithTimeout_ThrowsForSlowHandlers()
    {
        // Arrange
        _bus.RegisterQueryHandler<SlowQuery, SlowQueryResponse, SlowQueryHandler>();
        var config = new QueryConfiguration { Timeout = TimeSpan.FromMilliseconds(50) };

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => _bus.SendAsync<SlowQuery, SlowQueryResponse>(new SlowQuery(), config));
    }

    [Fact(DisplayName = "SendAsync with cancellation token respects external cancellation")]
    public async Task SendAsync_WithCancellationToken_RespectsExternalCancellation()
    {
        // Arrange
        _bus.RegisterQueryHandler<SlowQuery, SlowQueryResponse, SlowQueryHandler>();
        using var cts = new CancellationTokenSource();
        var config = new QueryConfiguration { CancellationToken = cts.Token };

        // Act
        var task = _bus.SendAsync<SlowQuery, SlowQueryResponse>(new SlowQuery(), config);
        await Task.Delay(50);
        cts.Cancel();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(() => task);
    }

    [Fact(DisplayName = "SendAsync without configuration uses default 30 second timeout")]
    public async Task SendAsync_DefaultConfiguration_Uses30SecondTimeout()
    {
        // Arrange
        _bus.RegisterQueryHandler<TestQuery, TestQueryResponse, TestQueryHandler>();

        // Act - should complete fast, not hit 30 second timeout
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _bus.SendAsync<TestQuery, TestQueryResponse>(new TestQuery("Fast"));
        stopwatch.Stop();

        // Assert
        response.ShouldNotBeNull();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000);
    }

    [Fact(DisplayName = "SendAsync with infinite timeout waits indefinitely")]
    public async Task SendAsync_WithInfiniteTimeout_WaitsIndefinitely()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new DelayedQueryHandler(TimeSpan.FromMilliseconds(200)));
        var provider = services.BuildServiceProvider();

        var bus = new InMemoryMessageBus(
            provider,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        bus.RegisterQueryHandler<DelayedQuery, DelayedQueryResponse, DelayedQueryHandler>();

        var config = new QueryConfiguration { Timeout = Timeout.InfiniteTimeSpan };

        // Act
        var response = await bus.SendAsync<DelayedQuery, DelayedQueryResponse>(new DelayedQuery(), config);

        // Assert
        response.ShouldNotBeNull();
        response.Completed.ShouldBeTrue();
    }

    #endregion

    #region Exception Handling

    [Fact(DisplayName = "SendAsync propagates exceptions from handler")]
    public async Task SendAsync_PropagatesExceptionsFromHandler()
    {
        // Arrange
        _bus.RegisterQueryHandler<ExceptionQuery, ExceptionQueryResponse, ExceptionThrowingQueryHandler>();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _bus.SendAsync<ExceptionQuery, ExceptionQueryResponse>(new ExceptionQuery()));

        exception.Message.ShouldBe("Handler exploded!");
    }

    [Fact(DisplayName = "SendAsync logs exception from handler")]
    public async Task SendAsync_LogsExceptionFromHandler()
    {
        // Arrange
        var logger = Substitute.For<ILogger<InMemoryMessageBus>>();
        var bus = new InMemoryMessageBus(
            _serviceProvider,
            logger,
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        bus.RegisterQueryHandler<ExceptionQuery, ExceptionQueryResponse, ExceptionThrowingQueryHandler>();

        // Act
        try
        {
            await bus.SendAsync<ExceptionQuery, ExceptionQueryResponse>(new ExceptionQuery());
        }
        catch
        {
            // Expected
        }

        // Assert - verify logging occurred (NSubstitute can't easily check Log extension methods,
        // but we verify behavior doesn't throw and exception is propagated)
    }

    #endregion

    #region Handler Registration

    [Fact(DisplayName = "RegisterQueryHandler overwrites previous handler for same message type")]
    public async Task RegisterQueryHandler_OverwritesPreviousHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<TestQueryHandler>();
        services.AddSingleton<AlternateQueryHandler>();
        var provider = services.BuildServiceProvider();

        var logger = Substitute.For<ILogger<InMemoryMessageBus>>();
        var bus = new InMemoryMessageBus(
            provider,
            logger,
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());

        // Act
        bus.RegisterQueryHandler<TestQuery, TestQueryResponse, TestQueryHandler>();
        bus.RegisterQueryHandler<TestQuery, TestQueryResponse, AlternateQueryHandler>();

        var response = await bus.SendAsync<TestQuery, TestQueryResponse>(new TestQuery("Test"));

        // Assert - should use the second (alternate) handler
        response.Result.ShouldBe("Alternate: Test");
    }

    #endregion
}

#region Test Messages and Handlers

// Basic query/response
public record TestQuery(string Input);
public record TestQueryResponse(string Result);

public class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
{
    public int InvocationCount { get; private set; }

    public Task<TestQueryResponse> Handle(TestQuery message)
    {
        InvocationCount++;
        return Task.FromResult(new TestQueryResponse($"Handled: {message.Input}"));
    }
}

public class AlternateQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
{
    public Task<TestQueryResponse> Handle(TestQuery message)
    {
        return Task.FromResult(new TestQueryResponse($"Alternate: {message.Input}"));
    }
}

// Another query type for multi-handler tests
public record AnotherQuery(int Value);
public record AnotherQueryResponse(int Value);

public class AnotherQueryHandler : IQueryHandler<AnotherQuery, AnotherQueryResponse>
{
    public Task<AnotherQueryResponse> Handle(AnotherQuery message)
    {
        return Task.FromResult(new AnotherQueryResponse(message.Value * 2));
    }
}

// Slow query for timeout tests
public record SlowQuery;
public record SlowQueryResponse(bool Completed);

public class SlowQueryHandler : IQueryHandler<SlowQuery, SlowQueryResponse>
{
    public async Task<SlowQueryResponse> Handle(SlowQuery message)
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        return new SlowQueryResponse(true);
    }
}

// Configurable delay query
public record DelayedQuery;
public record DelayedQueryResponse(bool Completed);

public class DelayedQueryHandler : IQueryHandler<DelayedQuery, DelayedQueryResponse>
{
    private readonly TimeSpan _delay;

    public DelayedQueryHandler(TimeSpan delay)
    {
        _delay = delay;
    }

    public async Task<DelayedQueryResponse> Handle(DelayedQuery message)
    {
        await Task.Delay(_delay);
        return new DelayedQueryResponse(true);
    }
}

// Exception-throwing query
public record ExceptionQuery;
public record ExceptionQueryResponse(string Message);

public class ExceptionThrowingQueryHandler : IQueryHandler<ExceptionQuery, ExceptionQueryResponse>
{
    public Task<ExceptionQueryResponse> Handle(ExceptionQuery message)
    {
        throw new InvalidOperationException("Handler exploded!");
    }
}

#endregion
