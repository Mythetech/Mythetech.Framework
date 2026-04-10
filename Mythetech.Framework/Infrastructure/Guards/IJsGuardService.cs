using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Guards;

/// <summary>
/// Provides a mechanism to gate rendering until a named JavaScript dependency is available.
/// Guards are registered client-side via <c>registerJsGuard(name, checkFn)</c> in js-guard.js.
/// </summary>
public interface IJsGuardService
{
    /// <summary>
    /// Returns true if the named guard has already resolved successfully.
    /// </summary>
    /// <param name="name">Guard name matching the name passed to registerJsGuard() in JavaScript.</param>
    bool IsReady(string name);

    /// <summary>
    /// Awaits the named guard's JavaScript promise, returning true when the dependency
    /// is available or false if it timed out. Successful results are cached; failures
    /// are not, so a re-render naturally retries.
    /// </summary>
    /// <param name="js">The JS runtime to invoke the guard check through.</param>
    /// <param name="name">Guard name matching the name passed to registerJsGuard() in JavaScript.</param>
    /// <param name="timeout">Overall timeout for both registration and readiness. Defaults to 15 seconds.</param>
    Task<bool> WaitForReadyAsync(IJSRuntime js, string name, TimeSpan? timeout = null);

    /// <summary>
    /// Clears any cached state for the named guard on both the C# and JavaScript side,
    /// so a subsequent WaitForReadyAsync call starts fresh. Used by the retry path.
    /// </summary>
    /// <param name="js">The JS runtime to invoke the clear through.</param>
    /// <param name="name">Guard name to reset.</param>
    Task ResetAsync(IJSRuntime js, string name);
}
