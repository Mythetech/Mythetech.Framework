using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Shell;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Desktop implementation of <see cref="IShellProcess"/>.
/// Wraps <see cref="Process"/> with proper event handling and cleanup.
/// </summary>
public class ShellProcess : IShellProcess
{
    private static readonly TimeSpan GroupPollInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan DisposeGrace = TimeSpan.FromSeconds(3);

    [UnsupportedOSPlatform("windows")]
    private static readonly int[] GroupStopSignals = [ProcessGroups.SIGINT, ProcessGroups.SIGTERM];

    private readonly Process _process;
    private readonly ILogger? _logger;
    private readonly bool _ownProcessGroup;

    // Read once at start, so a stop running alongside DisposeAsync can still reach the group.
    private int _processId;
    private bool _disposed;

    public ShellProcess(ProcessStartInfo startInfo, ILogger? logger = null)
        : this(startInfo, logger, ownProcessGroup: false)
    {
    }

    /// <param name="startInfo">How to start the process.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="ownProcessGroup">Whether <paramref name="startInfo"/> makes the process lead its own group.</param>
    public ShellProcess(ProcessStartInfo startInfo, ILogger? logger, bool ownProcessGroup)
    {
        _process = new Process { StartInfo = startInfo };
        _logger = logger;
        _ownProcessGroup = ownProcessGroup;

        _process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null) OutputReceived?.Invoke(args.Data);
        };

        _process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null) ErrorReceived?.Invoke(args.Data);
        };

        _process.Exited += (_, _) =>
        {
            try
            {
                Exited?.Invoke(_process.ExitCode);
            }
            catch
            {
                // Process may have been disposed
            }
        };

        _process.EnableRaisingEvents = true;
    }

    /// <inheritdoc />
    public int ProcessId => _processId;

    /// <inheritdoc />
    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public bool OwnsProcessGroup => _ownProcessGroup;

    // A process that was never started has no group, and pid 0 would reach the caller's own.
    [UnsupportedOSPlatformGuard("windows")]
    private bool SignalsItsGroup => _ownProcessGroup && _processId != 0 && !OperatingSystem.IsWindows();

    /// <inheritdoc />
    public bool HasExited
    {
        get
        {
            try
            {
                return _process.HasExited;
            }
            catch
            {
                return true;
            }
        }
    }

    /// <inheritdoc />
    public Stream StandardInput => _process.StandardInput.BaseStream;

    /// <inheritdoc />
    public event Action<string>? OutputReceived;

    /// <inheritdoc />
    public event Action<string>? ErrorReceived;

    /// <inheritdoc />
    public event Action<int>? Exited;

    internal void Start()
    {
        _process.Start();
        _processId = _process.Id;
        StartedAt = ReadStartTime();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private DateTimeOffset ReadStartTime()
    {
        try
        {
            return _process.StartTime.ToUniversalTime();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // A process that exits at once can be reaped before the OS is asked.
            _logger?.LogDebug(ex, "Could not read the start time of process {ProcessId}", _processId);
            return DateTimeOffset.MinValue;
        }
    }

    /// <inheritdoc />
    public async Task WriteInputAsync(string input, CancellationToken cancellationToken = default)
    {
        try
        {
            await _process.StandardInput.WriteAsync(input.AsMemory(), cancellationToken);
            await _process.StandardInput.FlushAsync(cancellationToken);
        }
        catch (IOException ex)
        {
            // Process may have exited - this is expected behavior
            _logger?.LogDebug(ex, "Failed to write to stdin - process may have exited");
        }
    }

    /// <inheritdoc />
    public async Task<int> WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        await _process.WaitForExitAsync(cancellationToken);
        return _process.ExitCode;
    }

    /// <inheritdoc />
    public void Interrupt()
    {
        if (HasExited) return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Windows: Send Ctrl+C via GenerateConsoleCtrlEvent
                if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
                {
                    // If Ctrl+C fails, fall back to kill
                    _logger?.LogDebug("GenerateConsoleCtrlEvent failed, falling back to Kill");
                    Kill(entireProcessTree: false);
                }
            }
            else if (!ProcessGroups.SignalProcess(_processId, ProcessGroups.SIGINT))
            {
                _logger?.LogWarning(
                    "Failed to interrupt process {ProcessId}: errno {Errno}", _processId, Marshal.GetLastWin32Error());
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to interrupt process {ProcessId}", _processId);
        }
    }

    /// <inheritdoc />
    public void Kill(bool entireProcessTree = true)
    {
        if (SignalsItsGroup)
        {
            KillGroup(entireProcessTree);
            return;
        }

        KillRoot(entireProcessTree);
    }

    /// <inheritdoc />
    /// <remarks>
    /// For a process that owns its group on Unix: SIGINT, then SIGTERM, then SIGKILL to the group, waiting up to
    /// <paramref name="grace"/> after each and returning once the group is empty or SIGKILL has been sent.
    /// </remarks>
    public async Task StopAsync(TimeSpan grace, CancellationToken cancellationToken = default)
    {
        if (!SignalsItsGroup)
        {
            await IShellProcess.InterruptThenKillAsync(this, grace, cancellationToken);
            return;
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(grace, TimeSpan.Zero);

        foreach (var signal in GroupStopSignals)
        {
            if (ProcessGroups.IsEmpty(_processId) || !MayStillOwnGroup())
                return;

            ProcessGroups.Signal(_processId, signal);

            if (await GroupEmptiesWithinAsync(grace, cancellationToken))
                return;
        }

        KillGroup(entireProcessTree: true);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            await StopAsync(DisposeGrace);
        }
        finally
        {
            _process.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private void KillRoot(bool entireProcessTree)
    {
        if (HasExited) return;

        try
        {
            _process.Kill(entireProcessTree);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to kill process {ProcessId}", _processId);
        }
    }

    // The leader's tree goes first, while it lives, so a descendant that left the group is reached too.
    [UnsupportedOSPlatform("windows")]
    private void KillGroup(bool entireProcessTree)
    {
        if (ProcessGroups.IsEmpty(_processId) || !MayStillOwnGroup())
            return;

        KillRoot(entireProcessTree);
        ProcessGroups.Signal(_processId, ProcessGroups.SIGKILL);
    }

    // A leader not yet reaped still holds its pid, so only a reaped leader's pid can have been reused.
    [UnsupportedOSPlatform("windows")]
    private bool MayStillOwnGroup() =>
        !HasExited || ProcessGroups.MayStillOwnGroup(_processId, StartedAt, ProcessGroups.StartTimeOf);

    [UnsupportedOSPlatform("windows")]
    private async Task<bool> GroupEmptiesWithinAsync(TimeSpan grace, CancellationToken cancellationToken)
    {
        var elapsed = Stopwatch.StartNew();
        while (!ProcessGroups.IsEmpty(_processId))
        {
            var remaining = grace - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero)
                return false;

            await Task.Delay(remaining < GroupPollInterval ? remaining : GroupPollInterval, cancellationToken);
        }

        return true;
    }

    #region Platform Signal Handling

    // Windows: Ctrl+C event
    private const uint CTRL_C_EVENT = 0;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

    #endregion
}
