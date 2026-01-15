using Microsoft.AspNetCore.Components;

namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// Base class to simplify registering components directly to the bus 
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public abstract class ComponentConsumer<TMessage> : ComponentBase, IConsumer<TMessage>, IDisposable where TMessage : class
{
    /// <summary>
    /// Message bus abstraction
    /// </summary>
    [Inject]
    protected IMessageBus MessageBus { get; set; } = default!;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        MessageBus.Subscribe(this);
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
        MessageBus.Unsubscribe(this);
        GC.SuppressFinalize(this);
    }
}