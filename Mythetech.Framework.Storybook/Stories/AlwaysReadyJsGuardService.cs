using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Guards;

namespace Mythetech.Framework.Storybook.Stories;

/// <summary>
/// Stub IJsGuardService that reports all guards as ready, for use in storybook stories.
/// </summary>
public class AlwaysReadyJsGuardService : IJsGuardService
{
    public bool IsReady(string name) => true;

    public Task<bool> WaitForReadyAsync(IJSRuntime js, string name, TimeSpan? timeout = null) => Task.FromResult(true);

    public Task ResetAsync(IJSRuntime js, string name) => Task.CompletedTask;
}
