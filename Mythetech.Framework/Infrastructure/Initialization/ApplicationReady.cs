namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// Published on the message bus on every launch once async initialization has finished. Apps typically run
/// initialization after the first render, so by then the UI is up as well: consume it to dismiss a splash
/// screen or start post-startup work. Apps that do not use <see cref="IAsyncInitializationHost"/> can
/// publish it themselves when they are ready.
/// </summary>
/// <param name="Hooks">The outcome of each initialization hook, in the order they ran.</param>
public sealed record ApplicationReady(IReadOnlyList<InitializationHookResult> Hooks);
