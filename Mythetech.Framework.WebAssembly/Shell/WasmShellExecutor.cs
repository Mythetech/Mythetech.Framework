using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Shell;

namespace Mythetech.Framework.WebAssembly.Shell;

/// <summary>
/// WebAssembly implementation of <see cref="IShellExecutor"/>.
/// Executes commands via registered handlers and JavaScript interop.
/// </summary>
public class WasmShellExecutor : IShellExecutor
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ICommandRegistry _commands;
    private readonly WasmShellOptions _options;
    private readonly ILogger<WasmShellExecutor>? _logger;

    public WasmShellExecutor(
        IJSRuntime jsRuntime,
        ICommandRegistry commands,
        WasmShellOptions options,
        ILogger<WasmShellExecutor>? logger = null)
    {
        _jsRuntime = jsRuntime;
        _commands = commands;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ShellResult> ExecuteAsync(ShellCommand command, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.Now;

        try
        {
            // 1. Check C# command registry first
            if (_commands.TryGetHandler(command.Command, out var handler))
            {
                _logger?.LogDebug("Executing registered C# command: {Command}", command.Command);
                var args = ParseArguments(command.Arguments);
                var result = await handler!(args, cancellationToken);
                return result with
                {
                    StartTime = startTime,
                    ExitTime = DateTimeOffset.Now,
                    Duration = DateTimeOffset.Now - startTime
                };
            }

            // 2. Check JavaScript command registry
            _logger?.LogDebug("Checking JS command registry for: {Command}", command.Command);
            var jsResult = await _jsRuntime.InvokeAsync<JsShellResult?>(
                "mythetech.shell.execute",
                cancellationToken,
                command.Command,
                command.Arguments,
                command.EnvironmentVariables);

            if (jsResult is { Found: true })
            {
                return new ShellResult
                {
                    ExitCode = jsResult.ExitCode,
                    StandardOutput = jsResult.StandardOutput ?? string.Empty,
                    StandardError = jsResult.StandardError ?? string.Empty,
                    StartTime = startTime,
                    ExitTime = DateTimeOffset.Now,
                    Duration = DateTimeOffset.Now - startTime
                };
            }

            // 3. Eval fallback (opt-in only)
            if (_options.AllowEval && command.Command.Equals("eval", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogDebug("Executing eval command with arguments: {Args}", command.Arguments);
                return await ExecuteEvalAsync(command.Arguments, startTime, cancellationToken);
            }

            // 4. Command not found
            _logger?.LogWarning("Command not found: {Command}", command.Command);
            return new ShellResult
            {
                ExitCode = 127,
                StandardOutput = string.Empty,
                StandardError = $"Command not found: {command.Command}",
                StartTime = startTime,
                ExitTime = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - startTime
            };
        }
        catch (OperationCanceledException)
        {
            return new ShellResult
            {
                ExitCode = 130, // Standard SIGINT exit code
                StandardOutput = string.Empty,
                StandardError = "Command cancelled",
                StartTime = startTime,
                ExitTime = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - startTime
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing command: {Command}", command.Command);
            return new ShellResult
            {
                ExitCode = 1,
                StandardOutput = string.Empty,
                StandardError = ex.Message,
                StartTime = startTime,
                ExitTime = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - startTime
            };
        }
    }

    /// <inheritdoc />
    public async Task<ShellResult> ExecuteStreamingAsync(
        ShellCommand command,
        Action<string>? onStdOut = null,
        Action<string>? onStdErr = null,
        CancellationToken cancellationToken = default)
    {
        // For WebAssembly, streaming is simulated - we execute and then invoke callbacks
        var result = await ExecuteAsync(command, cancellationToken);

        if (!string.IsNullOrEmpty(result.StandardOutput))
        {
            foreach (var line in result.StandardOutput.Split('\n'))
            {
                onStdOut?.Invoke(line);
            }
        }

        if (!string.IsNullOrEmpty(result.StandardError))
        {
            foreach (var line in result.StandardError.Split('\n'))
            {
                onStdErr?.Invoke(line);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public Task<IShellProcess> StartProcessAsync(ShellCommand command, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Starting interactive process: {Command}", command.Command);
        var process = new WasmShellProcess(_jsRuntime, _commands, _options, command, _logger);
        return Task.FromResult<IShellProcess>(process);
    }

    private async Task<ShellResult> ExecuteEvalAsync(
        string code,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>(
                "eval",
                cancellationToken,
                code);

            return new ShellResult
            {
                ExitCode = 0,
                StandardOutput = result ?? string.Empty,
                StandardError = string.Empty,
                StartTime = startTime,
                ExitTime = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - startTime
            };
        }
        catch (JSException ex)
        {
            return new ShellResult
            {
                ExitCode = 1,
                StandardOutput = string.Empty,
                StandardError = ex.Message,
                StartTime = startTime,
                ExitTime = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - startTime
            };
        }
    }

    private static string[] ParseArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return [];

        // Simple argument parsing - split on whitespace but respect quotes
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

    /// <summary>
    /// JavaScript interop result type.
    /// </summary>
    private sealed class JsShellResult
    {
        public bool Found { get; set; }
        public int ExitCode { get; set; }
        public string? StandardOutput { get; set; }
        public string? StandardError { get; set; }
    }
}
