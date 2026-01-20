using Microsoft.AspNetCore.Components;

namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// Base class to simplify registering components directly to the bus
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public abstract class ComponentConsumer<TMessage> : ComponentBase, IConsumer<TMessage>, IDisposable, IAsyncDisposable where TMessage : class
{
    private bool _disposed;

    /// <summary>
    /// Message bus abstraction
    /// </summary>
    [Inject]
    protected IMessageBus MessageBus { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        try
        {
            MessageBus.Subscribe(this);
        }
        catch
        {
            // Clean up if subscription fails
            MessageBus.Unsubscribe(this);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task Consume(TMessage message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    /// <summary>
    /// Overrideable consume method with the message and a cancellation token
    /// </summary>
    /// <param name="message">The subscribed message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected abstract Task Consume(TMessage message, CancellationToken cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        MessageBus.Unsubscribe(this);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        MessageBus.Unsubscribe(this);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Base class to simplify registering components directly to the bus for two message types
/// </summary>
public abstract class ComponentConsumer<T1, T2> : ComponentBase, IDisposable, IAsyncDisposable
    where T1 : class
    where T2 : class
{
    private sealed class Consumer1(ComponentConsumer<T1, T2> parent) : IConsumer<T1>
    {
        public Task Consume(T1 message) => parent.HandleMessage1(message);
    }

    private sealed class Consumer2(ComponentConsumer<T1, T2> parent) : IConsumer<T2>
    {
        public Task Consume(T2 message) => parent.HandleMessage2(message);
    }

    private bool _disposed;
    private IConsumer<T1>? _consumer1;
    private IConsumer<T2>? _consumer2;

    /// <summary>
    /// Message bus abstraction
    /// </summary>
    [Inject]
    protected IMessageBus MessageBus { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _consumer1 = new Consumer1(this);
        _consumer2 = new Consumer2(this);

        try
        {
            MessageBus.Subscribe(_consumer1);
            MessageBus.Subscribe(_consumer2);
        }
        catch
        {
            // Clean up any subscribed consumers if initialization fails
            if (_consumer1 is not null) MessageBus.Unsubscribe(_consumer1);
            if (_consumer2 is not null) MessageBus.Unsubscribe(_consumer2);
            throw;
        }
    }

    private async Task HandleMessage1(T1 message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    private async Task HandleMessage2(T2 message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    /// <summary>
    /// Consume the first message type
    /// </summary>
    protected abstract Task Consume(T1 message, CancellationToken cancellationToken);

    /// <summary>
    /// Consume the second message type
    /// </summary>
    protected abstract Task Consume(T2 message, CancellationToken cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_consumer1 is not null) MessageBus.Unsubscribe(_consumer1);
        if (_consumer2 is not null) MessageBus.Unsubscribe(_consumer2);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        if (_consumer1 is not null) MessageBus.Unsubscribe(_consumer1);
        if (_consumer2 is not null) MessageBus.Unsubscribe(_consumer2);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Base class to simplify registering components directly to the bus for three message types
/// </summary>
public abstract class ComponentConsumer<T1, T2, T3> : ComponentBase, IDisposable, IAsyncDisposable
    where T1 : class
    where T2 : class
    where T3 : class
{
    private sealed class Consumer1(ComponentConsumer<T1, T2, T3> parent) : IConsumer<T1>
    {
        public Task Consume(T1 message) => parent.HandleMessage1(message);
    }

    private sealed class Consumer2(ComponentConsumer<T1, T2, T3> parent) : IConsumer<T2>
    {
        public Task Consume(T2 message) => parent.HandleMessage2(message);
    }

    private sealed class Consumer3(ComponentConsumer<T1, T2, T3> parent) : IConsumer<T3>
    {
        public Task Consume(T3 message) => parent.HandleMessage3(message);
    }

    private bool _disposed;
    private IConsumer<T1>? _consumer1;
    private IConsumer<T2>? _consumer2;
    private IConsumer<T3>? _consumer3;

    /// <summary>
    /// Message bus abstraction
    /// </summary>
    [Inject]
    protected IMessageBus MessageBus { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _consumer1 = new Consumer1(this);
        _consumer2 = new Consumer2(this);
        _consumer3 = new Consumer3(this);

        try
        {
            MessageBus.Subscribe(_consumer1);
            MessageBus.Subscribe(_consumer2);
            MessageBus.Subscribe(_consumer3);
        }
        catch
        {
            // Clean up any subscribed consumers if initialization fails
            if (_consumer1 is not null) MessageBus.Unsubscribe(_consumer1);
            if (_consumer2 is not null) MessageBus.Unsubscribe(_consumer2);
            if (_consumer3 is not null) MessageBus.Unsubscribe(_consumer3);
            throw;
        }
    }

    private async Task HandleMessage1(T1 message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    private async Task HandleMessage2(T2 message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    private async Task HandleMessage3(T3 message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    /// <summary>
    /// Consume the first message type
    /// </summary>
    protected abstract Task Consume(T1 message, CancellationToken cancellationToken);

    /// <summary>
    /// Consume the second message type
    /// </summary>
    protected abstract Task Consume(T2 message, CancellationToken cancellationToken);

    /// <summary>
    /// Consume the third message type
    /// </summary>
    protected abstract Task Consume(T3 message, CancellationToken cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_consumer1 is not null) MessageBus.Unsubscribe(_consumer1);
        if (_consumer2 is not null) MessageBus.Unsubscribe(_consumer2);
        if (_consumer3 is not null) MessageBus.Unsubscribe(_consumer3);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        if (_consumer1 is not null) MessageBus.Unsubscribe(_consumer1);
        if (_consumer2 is not null) MessageBus.Unsubscribe(_consumer2);
        if (_consumer3 is not null) MessageBus.Unsubscribe(_consumer3);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Base class to simplify registering components directly to the bus for four message types
/// </summary>
public abstract class ComponentConsumer<T1, T2, T3, T4> : ComponentBase, IDisposable, IAsyncDisposable
    where T1 : class
    where T2 : class
    where T3 : class
    where T4 : class
{
    private sealed class Consumer1(ComponentConsumer<T1, T2, T3, T4> parent) : IConsumer<T1>
    {
        public Task Consume(T1 message) => parent.HandleMessage1(message);
    }

    private sealed class Consumer2(ComponentConsumer<T1, T2, T3, T4> parent) : IConsumer<T2>
    {
        public Task Consume(T2 message) => parent.HandleMessage2(message);
    }

    private sealed class Consumer3(ComponentConsumer<T1, T2, T3, T4> parent) : IConsumer<T3>
    {
        public Task Consume(T3 message) => parent.HandleMessage3(message);
    }

    private sealed class Consumer4(ComponentConsumer<T1, T2, T3, T4> parent) : IConsumer<T4>
    {
        public Task Consume(T4 message) => parent.HandleMessage4(message);
    }

    private bool _disposed;
    private IConsumer<T1>? _consumer1;
    private IConsumer<T2>? _consumer2;
    private IConsumer<T3>? _consumer3;
    private IConsumer<T4>? _consumer4;

    /// <summary>
    /// Message bus abstraction
    /// </summary>
    [Inject]
    protected IMessageBus MessageBus { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _consumer1 = new Consumer1(this);
        _consumer2 = new Consumer2(this);
        _consumer3 = new Consumer3(this);
        _consumer4 = new Consumer4(this);

        try
        {
            MessageBus.Subscribe(_consumer1);
            MessageBus.Subscribe(_consumer2);
            MessageBus.Subscribe(_consumer3);
            MessageBus.Subscribe(_consumer4);
        }
        catch
        {
            // Clean up any subscribed consumers if initialization fails
            if (_consumer1 is not null) MessageBus.Unsubscribe(_consumer1);
            if (_consumer2 is not null) MessageBus.Unsubscribe(_consumer2);
            if (_consumer3 is not null) MessageBus.Unsubscribe(_consumer3);
            if (_consumer4 is not null) MessageBus.Unsubscribe(_consumer4);
            throw;
        }
    }

    private async Task HandleMessage1(T1 message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    private async Task HandleMessage2(T2 message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    private async Task HandleMessage3(T3 message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    private async Task HandleMessage4(T4 message)
    {
        using var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token));
    }

    /// <summary>
    /// Consume the first message type
    /// </summary>
    protected abstract Task Consume(T1 message, CancellationToken cancellationToken);

    /// <summary>
    /// Consume the second message type
    /// </summary>
    protected abstract Task Consume(T2 message, CancellationToken cancellationToken);

    /// <summary>
    /// Consume the third message type
    /// </summary>
    protected abstract Task Consume(T3 message, CancellationToken cancellationToken);

    /// <summary>
    /// Consume the fourth message type
    /// </summary>
    protected abstract Task Consume(T4 message, CancellationToken cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_consumer1 is not null) MessageBus.Unsubscribe(_consumer1);
        if (_consumer2 is not null) MessageBus.Unsubscribe(_consumer2);
        if (_consumer3 is not null) MessageBus.Unsubscribe(_consumer3);
        if (_consumer4 is not null) MessageBus.Unsubscribe(_consumer4);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        if (_consumer1 is not null) MessageBus.Unsubscribe(_consumer1);
        if (_consumer2 is not null) MessageBus.Unsubscribe(_consumer2);
        if (_consumer3 is not null) MessageBus.Unsubscribe(_consumer3);
        if (_consumer4 is not null) MessageBus.Unsubscribe(_consumer4);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}