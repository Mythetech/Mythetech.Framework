#if NET11_0_OR_GREATER
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Ends the process groups that group commands lead once the app's process ends, a crash or a SIGKILL included, on macOS
/// and Linux. A watchdog shell outside the app's process tree reads a pipe only the app writes, and kills every group
/// still listed when that pipe closes, which the system does however the app ends.
/// </summary>
/// <remarks>Windows needs none: a group command's job object ends with the app's last handle to it.</remarks>
[UnsupportedOSPlatform("windows")]
internal sealed class ProcessGroupLifeline : IDisposable
{
    internal const string WatchdogName = "mythetech-process-lifeline";

    private static readonly TimeSpan PruneInterval = TimeSpan.FromSeconds(10);

    // "+N" lists group N and "-N" strikes it off. At the end of its input it kills every group listed, and again every
    // 100 ms for 2 s at most while any has a process left, since a process forked as its group is killed can escape.
    private const string WatchdogScript = """
        trap '' HUP INT
        groups=' '
        while IFS= read -r line; do
          case $line in
            +*) groups="$groups${line#+} " ;;
            -*) case $groups in *" ${line#-} "*) groups="${groups%% ${line#-} *} ${groups#* ${line#-} }" ;; esac ;;
          esac
        done
        tries=0
        while [ "$tries" -lt 20 ]; do
          left=''
          for group in $groups; do kill -s KILL -- "-$group" && left=1; done
          [ -z "$left" ] && break
          sleep 0.1
          tries=$((tries + 1))
        done
        """;

    // Starts the watchdog on this shell's input, prints its pid and exits, so the system adopts the watchdog.
    private const string StarterScript = """
        exec 3<&0
        /bin/sh -c "$1" "$0" <&3 >/dev/null 2>&1 3<&- &
        echo $!
        """;

    private readonly object _gate = new();
    private readonly Dictionary<int, DateTimeOffset> _groups = [];
    private readonly Func<int, DateTimeOffset?> _startTimeOf;
    private Process? _starter;
    private Timer? _pruning;
    private bool _unavailable;
    private bool _disposed;

    /// <summary>The lifeline of the app's own process, which no one disposes.</summary>
    public static ProcessGroupLifeline Shared { get; } = new();

    /// <param name="startTimeOf">Reads a pid's start time for the pid-reuse check; null for <see cref="ProcessGroups.StartTimeOf"/>.</param>
    internal ProcessGroupLifeline(Func<int, DateTimeOffset?>? startTimeOf = null)
    {
        _startTimeOf = startTimeOf ?? ProcessGroups.StartTimeOf;
    }

    /// <summary>The watchdog's pid, or null before a group is listed or when it could not start.</summary>
    internal int? WatchdogProcessId { get; private set; }

    /// <summary>
    /// Lists the group <paramref name="leaderPid"/> leads, so it is killed if the app ends before it does. Does nothing
    /// for a process in another group, so the app's own group and another's are never listed. A leader that has already
    /// gone is listed, since what it started can still be in its group.
    /// </summary>
    /// <param name="leaderPid">The group leader, whose pid is the group's id.</param>
    /// <param name="leaderStartedAt">When the leader started, for the check that its pid was not reused.</param>
    /// <param name="logger">Told, once, when the watchdog cannot start.</param>
    public void Adopt(int leaderPid, DateTimeOffset leaderStartedAt, ILogger? logger = null)
    {
        if (leaderPid <= 1)
            return;

        var group = ProcessGroups.GroupOf(leaderPid);
        if ((group != leaderPid && group != -1) || leaderPid == ProcessGroups.GroupOf(System.Environment.ProcessId))
            return;

        lock (_gate)
        {
            if (_disposed)
                return;

            _groups[leaderPid] = leaderStartedAt;
            Tell($"+{leaderPid}", logger);
            _pruning ??= new Timer(_ => Prune(), null, PruneInterval, PruneInterval);
        }
    }

    /// <summary>Strikes a group off the list, as once it has been stopped, so a crash after that never signals its id.</summary>
    public void Release(int processGroupId)
    {
        lock (_gate)
        {
            if (_groups.Remove(processGroupId))
                Tell($"-{processGroupId}", logger: null);
        }
    }

    /// <summary>Whether the group is listed.</summary>
    internal bool Holds(int processGroupId)
    {
        lock (_gate)
        {
            return _groups.ContainsKey(processGroupId);
        }
    }

    /// <summary>Strikes off every group with no process left, or whose leader's pid now belongs to another process.</summary>
    internal void Prune()
    {
        lock (_gate)
        {
            foreach (var (group, startedAt) in _groups.ToArray())
            {
                if (ProcessGroups.IsEmpty(group) || !ProcessGroups.MayStillOwnGroup(group, startedAt, _startTimeOf))
                {
                    _groups.Remove(group);
                    Tell($"-{group}", logger: null);
                }
            }
        }
    }

    /// <summary>Closes the pipe as the app's end would, so the watchdog kills every group still listed.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _pruning?.Dispose();
            CloseStarter();
        }
    }

    // Under the gate.
    private void Tell(string line, ILogger? logger)
    {
        if (_disposed)
            return;

        if (_starter is null && !_unavailable)
        {
            Start(logger);
            return;
        }

        if (_starter is null)
            return;

        try
        {
            _starter.StandardInput.Write(line + "\n");
            _starter.StandardInput.Flush();
        }
        catch (IOException)
        {
            // Only a watchdog that has gone refuses the write, so its successor is given every group listed.
            CloseStarter();
            Start(logger);
        }
    }

    // Under the gate. Lists every group the app holds, so a restarted watchdog is told nothing twice.
    private void Start(ILogger? logger)
    {
        var startInfo = new ProcessStartInfo("/bin/sh", ["-c", StarterScript, WatchdogName, WatchdogScript])
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            // A session of its own, so a terminal's hangup or Ctrl+C, or a kill of the app's group, leaves it to do its work.
            StartDetached = true,
        };

        try
        {
            var starter = Process.Start(startInfo) ?? throw new InvalidOperationException("The watchdog did not start");
            _starter = starter;
            WatchdogProcessId = int.Parse(
                starter.StandardOutput.ReadLine() ?? throw new InvalidOperationException("The watchdog printed no pid"),
                CultureInfo.InvariantCulture);

            foreach (var group in _groups.Keys)
                starter.StandardInput.Write($"+{group}\n");
            starter.StandardInput.Flush();
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException or FormatException)
        {
            CloseStarter();
            _unavailable = true;
            logger?.LogWarning(ex, "The process group watchdog could not start, so a crash of the app leaves its group commands running");
        }
    }

    // Process.Dispose leaves the redirected streams open, so the pipe is closed by hand.
    private void CloseStarter()
    {
        if (_starter is null)
            return;

        try
        {
            _starter.StandardInput.Close();
        }
        catch (IOException)
        {
            // The watchdog has gone, so closing loses nothing.
        }

        _starter.Dispose();
        _starter = null;
        WatchdogProcessId = null;
    }
}
#endif
