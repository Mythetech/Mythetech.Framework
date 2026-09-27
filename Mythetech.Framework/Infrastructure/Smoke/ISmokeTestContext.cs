namespace Mythetech.Framework.Infrastructure.Smoke;

/// <summary>
/// Tells code above the desktop host whether this process is a smoke run, so services and components
/// can switch off side effects such as telemetry or first-run prompts. Desktop apps get one backed by the
/// host's smoke switch; where none is registered, treat the run as a normal one.
/// </summary>
public interface ISmokeTestContext
{
    /// <summary>
    /// Whether this process is a smoke run.
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// The context for hosts without smoke mode, such as WebAssembly.
/// </summary>
internal sealed class DisabledSmokeTestContext : ISmokeTestContext
{
    public static DisabledSmokeTestContext Instance { get; } = new();

    public bool IsEnabled => false;
}
