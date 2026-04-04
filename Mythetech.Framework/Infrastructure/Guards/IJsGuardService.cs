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
    /// is available or false if it timed out.
    /// </summary>
    /// <param name="js">The JS runtime to invoke the guard check through.</param>
    /// <param name="name">Guard name matching the name passed to registerJsGuard() in JavaScript.</param>
    Task<bool> WaitForReadyAsync(IJSRuntime js, string name);
}
