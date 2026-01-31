using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Shell;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Desktop implementation of <see cref="IShellProcess"/>.
/// Wraps <see cref="Process"/> with proper event handling and cleanup.
/// </summary>
public class ShellProcess : IShellProcess
{
    private readonly Process _process;
    private readonly ILogger? _logger;
    private bool _disposed;

    public ShellProcess(ProcessStartInfo startInfo, ILogger? logger = null)
    {
        _process = new Process { StartInfo = startInfo };
        _logger = logger;

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
    public int ProcessId => _process.Id;

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
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
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
            else
            {
                // Unix: Send SIGINT
                SendSignal(_process.Id, Signal.SIGINT);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to interrupt process {ProcessId}", _process.Id);
        }
    }

    /// <inheritdoc />
    public void Kill(bool entireProcessTree = true)
    {
        if (HasExited) return;

        try
        {
            _process.Kill(entireProcessTree);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to kill process {ProcessId}", _process.Id);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (!HasExited)
            {
                // Try graceful shutdown first
                Interrupt();

                // Wait a bit for graceful exit
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                try
                {
                    await _process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Force kill if graceful shutdown didn't work
                    Kill(entireProcessTree: true);
                }
            }
        }
        finally
        {
            _process.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    #region Platform Signal Handling

    private enum Signal
    {
        SIGINT = 2,
        SIGTERM = 15,
        SIGKILL = 9
    }

    private static void SendSignal(int pid, Signal signal)
    {
        if (OperatingSystem.IsWindows())
            return;

        // P/Invoke to send signal on Unix
        var result = kill(pid, (int)signal);
        if (result != 0)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to send signal {signal} to process {pid}. Error: {error}");
        }
    }

    // Unix: libc kill()
    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    // Windows: Ctrl+C event
    private const uint CTRL_C_EVENT = 0;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

    #endregion
}
