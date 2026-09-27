namespace Mythetech.Framework.Infrastructure.Smoke;

/// <summary>
/// A named check that runs once in a smoke run, after the first render and after startup initialization
/// has finished. Checks are resolved from the page's service scope, so they see the same scoped services
/// as the UI, and they are never created outside a smoke run.
/// </summary>
public interface ISmokeCheck
{
    /// <summary>
    /// Name shown in the verdict, prefixed by layer: <c>framework/...</c> for Framework checks and
    /// <c>&lt;app&gt;/...</c> for app checks, for example <c>horizon/workspace-store</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// How long the check may run before it fails. Defaults to 10 seconds.
    /// </summary>
    TimeSpan Timeout => TimeSpan.FromSeconds(10);

    /// <summary>
    /// Runs the check. Throw to fail it.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the check's timeout or the run's budget runs out.</param>
    Task RunAsync(CancellationToken cancellationToken);
}
