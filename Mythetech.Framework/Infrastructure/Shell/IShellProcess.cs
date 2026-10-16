using System.Diagnostics;

namespace Mythetech.Framework.Infrastructure.Shell;

/// <summary>
/// Handle for an interactive process with stdin access.
/// </summary>
public interface IShellProcess : IAsyncDisposable
{
    /// <summary>
    /// The process ID.
    /// </summary>
    int ProcessId { get; }

    /// <summary>
    /// When the process started, in UTC, as the OS records it, or <see cref="DateTimeOffset.MinValue"/> when the
    /// OS can't say. Precision varies by OS (microseconds on macOS, clock ticks on Linux), so compare with a tolerance.
    /// </summary>
    /// <remarks>The default reads the OS by <see cref="ProcessId"/> on every call.</remarks>
    DateTimeOffset StartedAt
    {
        get
        {
            if (OperatingSystem.IsBrowser())
                return DateTimeOffset.MinValue;

            try
            {
                using var process = Process.GetProcessById(ProcessId);
                return process.StartTime.ToUniversalTime();
            }
            catch (Exception)
            {
                return DateTimeOffset.MinValue;
            }
        }
    }

    /// <summary>
    /// Whether the process leads its own process group. See <see cref="ShellCommand.OwnProcessGroup"/>.
    /// </summary>
    bool OwnsProcessGroup => false;

    /// <summary>
    /// Whether the process has exited.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// The standard input stream for writing to the process.
    /// </summary>
    Stream StandardInput { get; }

    /// <summary>
    /// Event raised when output is received from stdout.
    /// </summary>
    event Action<string>? OutputReceived;

    /// <summary>
    /// Event raised when output is received from stderr.
    /// </summary>
    event Action<string>? ErrorReceived;

    /// <summary>
    /// Event raised when the process exits.
    /// </summary>
    event Action<int>? Exited;

    /// <summary>
    /// Writes input to the process stdin.
    /// </summary>
    Task WriteInputAsync(string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for the process to exit and returns the exit code.
    /// It also waits for every descendant still holding stdout or stderr, even for a group leader; a caller that needs
    /// only the leader's exit uses <see cref="Exited"/>, then <see cref="StopAsync"/> to end the rest of the group.
    /// </summary>
    Task<int> WaitForExitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an interrupt signal (SIGINT on Unix, Ctrl+C on Windows).
    /// Allows the process to perform cleanup before exiting.
    /// </summary>
    void Interrupt();

    /// <summary>
    /// Forcefully kills the process. A process that owns its group kills the whole group too.
    /// </summary>
    /// <param name="entireProcessTree">
    /// Whether to kill child processes as well. It does not spare a group leader's group, which is always killed; on Unix
    /// it only decides whether a descendant that left the group is reached through the leader's tree while the leader lives.
    /// </param>
    void Kill(bool entireProcessTree = true);

    /// <summary>
    /// Stops the process: interrupts it, waits up to <paramref name="grace"/> for it to exit, then kills its tree.
    /// A process that owns its group stops the whole group instead, even after the process itself has exited.
    /// </summary>
    /// <param name="grace">How long to wait after each step before the next, harsher one.</param>
    /// <param name="cancellationToken">Ends the wait; the stop then throws and goes no further.</param>
    Task StopAsync(TimeSpan grace, CancellationToken cancellationToken = default) =>
        InterruptThenKillAsync(this, grace, cancellationToken);

    /// <summary>
    /// The default <see cref="StopAsync"/>, shared with implementations that stop a process without a group the same way.
    /// </summary>
    internal static async Task InterruptThenKillAsync(IShellProcess process, TimeSpan grace, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(grace, TimeSpan.Zero);

        if (process.HasExited)
            return;

        process.Interrupt();

        using var graceTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        graceTimeout.CancelAfter(grace);
        try
        {
            await process.WaitForExitAsync(graceTimeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
        }
    }
}
