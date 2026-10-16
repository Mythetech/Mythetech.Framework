using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// POSIX process group queries and signals through libc.
/// </summary>
[UnsupportedOSPlatform("windows")]
internal static class ProcessGroups
{
    public const int SIGINT = 2;
    public const int SIGKILL = 9;
    public const int SIGTERM = 15;

    private const int ESRCH = 3;

    // Start times are recorded in microseconds on macOS and in clock ticks on Linux.
    private static readonly TimeSpan StartTimeTolerance = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether no process is left in the group. A group the caller may not signal (EPERM) still has members.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The id is 1 or less: kill(-1) checks every process the user owns, and kill(0) the caller's own group.
    /// </exception>
    public static bool IsEmpty(int processGroupId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(processGroupId, 1);
        if (kill(-processGroupId, 0) == 0)
            return false;

        return Marshal.GetLastWin32Error() == ESRCH;
    }

    /// <summary>
    /// The process group of <paramref name="pid"/>, or -1 when no such process exists.
    /// </summary>
    public static int GroupOf(int pid) => getpgid(pid);

    /// <summary>
    /// Sends <paramref name="signal"/> to every process in the group. False when none could be signalled,
    /// as when the group is empty. Any failure but an empty group is logged to <paramref name="logger"/> at
    /// <paramref name="failureLevel"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The id is 1 or less: kill(-1) signals every process the user owns, and kill(0) the caller's own group.
    /// </exception>
    public static bool Signal(int processGroupId, int signal, ILogger? logger = null, LogLevel failureLevel = LogLevel.Debug)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(processGroupId, 1);
        if (kill(-processGroupId, signal) == 0)
            return true;

        var errno = Marshal.GetLastWin32Error();
        if (errno != ESRCH)
        {
            logger?.Log(
                failureLevel,
                "Failed to send signal {Signal} to process group {ProcessGroupId}: errno {Errno}",
                signal,
                processGroupId,
                errno);
        }
        return false;
    }

    /// <summary>
    /// Sends <paramref name="signal"/> to the one process <paramref name="pid"/>. False when it could not be
    /// signalled; <see cref="Marshal.GetLastWin32Error"/> then holds the errno.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The pid is 0 or less, which kill() reads as a group.</exception>
    public static bool SignalProcess(int pid, int signal)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pid);
        return kill(pid, signal) == 0;
    }

    /// <summary>
    /// When <paramref name="pid"/> started, in UTC. Null when no such process exists, and
    /// <see cref="DateTimeOffset.MinValue"/> when it exists but the OS won't say, as for another user's process.
    /// </summary>
    internal static DateTimeOffset? StartTimeOf(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.StartTime.ToUniversalTime();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return null;
        }
        catch (Win32Exception)
        {
            return DateTimeOffset.MinValue;
        }
    }

    /// <summary>
    /// Whether the group that <paramref name="leaderPid"/> led may still be the one it started. False once a
    /// process with another start time holds the pid: the number was reused, so the group is someone else's.
    /// Also false when a process holds the pid and either start time is <see cref="DateTimeOffset.MinValue"/>,
    /// since an unreadable time can't show the two are the same process.
    /// </summary>
    internal static bool MayStillOwnGroup(int leaderPid, DateTimeOffset leaderStartedAt, Func<int, DateTimeOffset?> startTimeOf)
    {
        if (startTimeOf(leaderPid) is not { } startedAt)
            return true;

        return startedAt != DateTimeOffset.MinValue
            && leaderStartedAt != DateTimeOffset.MinValue
            && (startedAt - leaderStartedAt).Duration() <= StartTimeTolerance;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    [DllImport("libc", SetLastError = true)]
    private static extern int getpgid(int pid);
}
