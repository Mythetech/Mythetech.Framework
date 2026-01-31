using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Shell;

namespace Mythetech.Framework.WebAssembly.Shell;

/// <summary>
/// WebAssembly implementation of <see cref="IShellProcess"/>.
/// Simulates interactive process behavior via JavaScript interop.
/// </summary>
public sealed class WasmShellProcess : IShellProcess
{
    private static int _nextProcessId = 1;

    private readonly IJSRuntime _jsRuntime;
    private readonly ICommandRegistry _commands;
    private readonly WasmShellOptions _options;
    private readonly ShellCommand _command;
    private readonly ILogger? _logger;
    private readonly DotNetObjectReference<WasmShellProcess> _dotNetRef;
    private readonly TaskCompletionSource<int> _exitTcs = new();
    private readonly WasmStdinStream _stdinStream;
    private readonly CancellationTokenSource _cts = new();

    private int? _jsProcessHandle;
    private bool _disposed;

    /// <inheritdoc />
    public int ProcessId { get; }

    /// <inheritdoc />
    public bool HasExited { get; private set; }

    /// <inheritdoc />
    public Stream StandardInput => _stdinStream;

    /// <inheritdoc />
    public event Action<string>? OutputReceived;

    /// <inheritdoc />
    public event Action<string>? ErrorReceived;

    /// <inheritdoc />
    public event Action<int>? Exited;

    internal WasmShellProcess(
        IJSRuntime jsRuntime,
        ICommandRegistry commands,
        WasmShellOptions options,
        ShellCommand command,
        ILogger? logger)
    {
        _jsRuntime = jsRuntime;
        _commands = commands;
        _options = options;
        _command = command;
        _logger = logger;
        _dotNetRef = DotNetObjectReference.Create(this);
        _stdinStream = new WasmStdinStream(this);

        ProcessId = Interlocked.Increment(ref _nextProcessId);

        // Start the process asynchronously
        _ = StartAsync();
    }

    private async Task StartAsync()
    {
        try
        {
            // Try to create a JS process handle
            _jsProcessHandle = await _jsRuntime.InvokeAsync<int?>(
                "mythetech.shell.createProcess",
                _cts.Token,
                _command.Command,
                _dotNetRef);

            if (_jsProcessHandle is null)
            {
                // No JS handler - check if we have a C# handler
                if (_commands.TryGetHandler(_command.Command, out var handler))
                {
                    // Run the C# handler
                    var args = ParseArguments(_command.Arguments);
                    var result = await handler!(args, _cts.Token);

                    if (!string.IsNullOrEmpty(result.StandardOutput))
                        RaiseOutputReceived(result.StandardOutput);
                    if (!string.IsNullOrEmpty(result.StandardError))
                        RaiseErrorReceived(result.StandardError);

                    CompleteWithExitCode(result.ExitCode);
                }
                else
                {
                    // Command not found
                    RaiseErrorReceived($"Command not found: {_command.Command}");
                    CompleteWithExitCode(127);
                }
            }
        }
        catch (OperationCanceledException)
        {
            CompleteWithExitCode(130);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting process: {Command}", _command.Command);
            RaiseErrorReceived(ex.Message);
            CompleteWithExitCode(1);
        }
    }

    /// <summary>
    /// Called from JavaScript when output is received.
    /// </summary>
    [JSInvokable]
    public void OnOutput(string data)
    {
        if (!_disposed)
            RaiseOutputReceived(data);
    }

    /// <summary>
    /// Called from JavaScript when error output is received.
    /// </summary>
    [JSInvokable]
    public void OnError(string data)
    {
        if (!_disposed)
            RaiseErrorReceived(data);
    }

    /// <summary>
    /// Called from JavaScript when the process exits.
    /// </summary>
    [JSInvokable]
    public void OnExit(int exitCode)
    {
        CompleteWithExitCode(exitCode);
    }

    /// <inheritdoc />
    public async Task WriteInputAsync(string input, CancellationToken cancellationToken = default)
    {
        if (_disposed || HasExited)
            return;

        if (_jsProcessHandle is not null)
        {
            await _jsRuntime.InvokeVoidAsync(
                "mythetech.shell.writeInput",
                cancellationToken,
                _jsProcessHandle,
                input);
        }
    }

    /// <inheritdoc />
    public Task<int> WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        if (HasExited)
            return _exitTcs.Task;

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        linkedCts.Token.Register(() =>
        {
            if (!HasExited)
                _exitTcs.TrySetCanceled(linkedCts.Token);
        });

        return _exitTcs.Task;
    }

    /// <inheritdoc />
    public void Interrupt()
    {
        if (_disposed || HasExited)
            return;

        _logger?.LogDebug("Interrupting process {ProcessId}", ProcessId);

        if (_jsProcessHandle is not null)
        {
            _ = _jsRuntime.InvokeVoidAsync("mythetech.shell.interrupt", _jsProcessHandle);
        }

        _cts.Cancel();
    }

    /// <inheritdoc />
    public void Kill(bool entireProcessTree = true)
    {
        if (_disposed || HasExited)
            return;

        _logger?.LogDebug("Killing process {ProcessId}", ProcessId);

        if (_jsProcessHandle is not null)
        {
            _ = _jsRuntime.InvokeVoidAsync("mythetech.shell.kill", _jsProcessHandle);
        }

        _cts.Cancel();
        CompleteWithExitCode(-1);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Try to gracefully stop if still running
        if (!HasExited)
        {
            Interrupt();

            // Give it a moment to clean up
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            try
            {
                await _exitTcs.Task.WaitAsync(timeoutCts.Token);
            }
            catch
            {
                Kill();
            }
        }

        if (_jsProcessHandle is not null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("mythetech.shell.dispose", _jsProcessHandle);
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _dotNetRef.Dispose();
        _cts.Dispose();
        await _stdinStream.DisposeAsync();
    }

    private void RaiseOutputReceived(string data)
    {
        try
        {
            OutputReceived?.Invoke(data);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in OutputReceived handler");
        }
    }

    private void RaiseErrorReceived(string data)
    {
        try
        {
            ErrorReceived?.Invoke(data);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in ErrorReceived handler");
        }
    }

    private void CompleteWithExitCode(int exitCode)
    {
        if (HasExited)
            return;

        HasExited = true;
        _exitTcs.TrySetResult(exitCode);

        try
        {
            Exited?.Invoke(exitCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in Exited handler");
        }
    }

    private static string[] ParseArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return [];

        var args = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';

        foreach (var c in arguments)
        {
            if (!inQuotes && (c == '"' || c == '\''))
            {
                inQuotes = true;
                quoteChar = c;
            }
            else if (inQuotes && c == quoteChar)
            {
                inQuotes = false;
                quoteChar = '\0';
            }
            else if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            args.Add(current.ToString());

        return [.. args];
    }
}

/// <summary>
/// A stream that writes to the process stdin via the parent WasmShellProcess.
/// </summary>
internal sealed class WasmStdinStream : Stream
{
    private readonly WasmShellProcess _process;
    private bool _disposed;

    public WasmStdinStream(WasmShellProcess process)
    {
        _process = process;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WasmStdinStream));

        var text = System.Text.Encoding.UTF8.GetString(buffer, offset, count);
        _ = _process.WriteInputAsync(text);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WasmStdinStream));

        var text = System.Text.Encoding.UTF8.GetString(buffer, offset, count);
        await _process.WriteInputAsync(text, cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WasmStdinStream));

        var text = System.Text.Encoding.UTF8.GetString(buffer.Span);
        await _process.WriteInputAsync(text, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        _disposed = true;
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
