namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// The outcome of one <see cref="IAsyncInitializationHook"/> in an initialization run.
/// </summary>
/// <param name="Name">The hook's name.</param>
/// <param name="Order">The hook's order.</param>
/// <param name="Duration">How long the hook ran.</param>
/// <param name="Error">The exception the hook threw, or null when it succeeded.</param>
public sealed record InitializationHookResult(string Name, int Order, TimeSpan Duration, Exception? Error)
{
    /// <summary>
    /// Whether the hook completed without throwing.
    /// </summary>
    public bool Succeeded => Error is null;
}
