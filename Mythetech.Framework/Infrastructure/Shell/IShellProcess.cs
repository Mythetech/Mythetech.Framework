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
    /// </summary>
    Task<int> WaitForExitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an interrupt signal (SIGINT on Unix, Ctrl+C on Windows).
    /// Allows the process to perform cleanup before exiting.
    /// </summary>
    void Interrupt();

    /// <summary>
    /// Forcefully kills the process.
    /// </summary>
    /// <param name="entireProcessTree">Whether to kill child processes as well.</param>
    void Kill(bool entireProcessTree = true);
}
