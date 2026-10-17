using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Shell;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Desktop implementation of <see cref="IShellExecutor"/>.
/// Handles platform-specific shell configuration for proper PATH setup.
/// </summary>
public partial class ShellExecutor : IShellExecutor
{
    private static readonly TimeSpan GroupStopGrace = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan OutputDrainTimeout = TimeSpan.FromSeconds(1);

    private readonly ILogger<ShellExecutor>? _logger;

    /// <summary>
    /// Setup script that ensures proper PATH on macOS.
    /// Runs path_helper, brew shellenv, and sources user profiles.
    /// GUI apps launched from Finder don't inherit shell environment,
    /// so we must explicitly set this up.
    /// </summary>
    private const string MacOsSetupScript =
        "eval \"$(/usr/libexec/path_helper -s 2>/dev/null)\"; " +
        "eval \"$(/opt/homebrew/bin/brew shellenv 2>/dev/null || /usr/local/bin/brew shellenv 2>/dev/null)\"; " +
        "source ~/.zprofile 2>/dev/null; " +
        "source ~/.zshrc 2>/dev/null; ";

    /// <summary>
    /// Setup script for Linux (bash) that sources common profile files.
    /// </summary>
    private const string LinuxSetupScript =
        "source /etc/profile 2>/dev/null; " +
        "source ~/.profile 2>/dev/null; " +
        "source ~/.bashrc 2>/dev/null; ";

    /// <summary>
    /// Setup script for POSIX sh (fallback for minimal distros like Alpine).
    /// Uses . instead of source for POSIX compatibility.
    /// </summary>
    private const string ShSetupScript =
        ". /etc/profile 2>/dev/null; " +
        ". ~/.profile 2>/dev/null; ";

    public ShellExecutor(ILogger<ShellExecutor>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ShellResult> ExecuteAsync(
        ShellCommand command,
        CancellationToken cancellationToken = default)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var result = await ExecuteStreamingAsync(
            command,
            line => stdout.AppendLine(line),
            line => stderr.AppendLine(line),
            cancellationToken);

        return result with
        {
            StandardOutput = stdout.ToString().TrimEnd(),
            StandardError = stderr.ToString().TrimEnd()
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// A command with <see cref="ShellCommand.OwnProcessGroup"/> doesn't wait for something it started that still holds
    /// its output. Once the command itself exits, its group is stopped as <see cref="IShellProcess.StopAsync"/> stops it,
    /// so nothing it started is left running; a timeout or cancellation stops the group the same way. The output is then
    /// drained for at most 1 second before the call returns or throws, so lines a slow callback has not yet taken may be
    /// dropped. The callbacks are unsubscribed before it returns or throws, so a process that left the group and still
    /// holds the output does not reach them afterwards.
    /// </remarks>
    public async Task<ShellResult> ExecuteStreamingAsync(
        ShellCommand command,
        Action<string>? onStdOut = null,
        Action<string>? onStdErr = null,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = command.Timeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;

        if (command.Timeout.HasValue && timeoutCts != null)
        {
            timeoutCts.CancelAfter(command.Timeout.Value);
        }

        var effectiveCt = timeoutCts?.Token ?? cancellationToken;

        if (command.OwnProcessGroup)
        {
            return await ExecuteInOwnGroupAsync(command, onStdOut, onStdErr, effectiveCt);
        }

        var startInfo = CreateProcessStartInfo(command);
        using var process = new Process { StartInfo = startInfo };

        var startTime = DateTimeOffset.Now;

        if (onStdOut != null)
        {
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data != null) onStdOut(args.Data);
            };
        }

        if (onStdErr != null)
        {
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data != null) onStdErr(args.Data);
            };
        }

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(effectiveCt);

            var exitTime = DateTimeOffset.Now;

            return new ShellResult
            {
                ExitCode = process.ExitCode,
                StartTime = startTime,
                ExitTime = exitTime,
                Duration = exitTime - startTime
            };
        }
        catch (OperationCanceledException) when (effectiveCt.IsCancellationRequested)
        {
            // Try graceful kill first, then force
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Process may have already exited
            }

            throw;
        }
    }

    private async Task<ShellResult> ExecuteInOwnGroupAsync(
        ShellCommand command,
        Action<string>? onStdOut,
        Action<string>? onStdErr,
        CancellationToken cancellationToken)
    {
        await using var process = new ShellProcess(CreateProcessStartInfo(command), _logger, ownProcessGroup: true);

        if (onStdOut != null) process.OutputReceived += onStdOut;
        if (onStdErr != null) process.ErrorReceived += onStdErr;

        var startTime = DateTimeOffset.Now;
        try
        {
            process.Start();
            var exitCode = await process.WaitForLeaderExitAsync(cancellationToken);
            var exitTime = DateTimeOffset.Now;

            await StopGroupAndDrainAsync(process);

            return new ShellResult
            {
                ExitCode = exitCode,
                StartTime = startTime,
                ExitTime = exitTime,
                Duration = exitTime - startTime
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await StopGroupAndDrainAsync(process);
            throw;
        }
        finally
        {
            // A process that left the group can still hold the output after the drain gives up on it.
            if (onStdOut != null) process.OutputReceived -= onStdOut;
            if (onStdErr != null) process.ErrorReceived -= onStdErr;
        }
    }

    // The stop is not cancellable: the caller's token may be the reason for it.
    private static async Task StopGroupAndDrainAsync(ShellProcess process)
    {
        await process.StopAsync(GroupStopGrace, CancellationToken.None);

        using var drain = new CancellationTokenSource(OutputDrainTimeout);
        try
        {
            await process.WaitForExitAsync(drain.Token);
        }
        catch (OperationCanceledException) when (drain.IsCancellationRequested)
        {
            // A process that left the group still holds the output, so the rest of it is not waited for.
        }
    }

    /// <inheritdoc />
    public Task<IShellProcess> StartProcessAsync(
        ShellCommand command,
        CancellationToken cancellationToken = default)
    {
        var startInfo = CreateProcessStartInfo(command, redirectInput: true);
        var shellProcess = new ShellProcess(startInfo, _logger, command.OwnProcessGroup);
        shellProcess.Start();
        return Task.FromResult<IShellProcess>(shellProcess);
    }

    internal ProcessStartInfo CreateProcessStartInfo(ShellCommand command, bool redirectInput = false)
    {
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = redirectInput,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(command.WorkingDirectory))
        {
            startInfo.WorkingDirectory = command.WorkingDirectory;
        }

        if (command.OwnProcessGroup && OperatingSystem.IsWindows())
        {
            // Held until it is in its job, so nothing it starts escapes. StartDetached here would mean DETACHED_PROCESS.
            startInfo.StartSuspended = true;
        }
        else if (command.OwnProcessGroup)
        {
            // The child calls setsid() before exec and keeps its redirected stdio.
            startInfo.StartDetached = true;

            // .NET forks these children from one long-lived thread, so PR_SET_PDEATHSIG fires only when the app exits.
            if (OperatingSystem.IsLinux())
            {
                startInfo.KillOnParentExit = true;
            }
        }

        if (command.UseShell)
        {
            ConfigureWithShell(startInfo, command);
        }
        else
        {
            ConfigureDirectExecution(startInfo, command);
        }

        return startInfo;
    }

    private void ConfigureWithShell(ProcessStartInfo startInfo, ShellCommand command)
    {
        var envExports = BuildEnvironmentExports(command.EnvironmentVariables);

        if (OperatingSystem.IsMacOS())
        {
            startInfo.FileName = "/bin/zsh";
            startInfo.ArgumentList.Add("-c");

            startInfo.ArgumentList.Add($"{MacOsSetupScript}{envExports}{BuildPosixCommandLine(command)}");
        }
        else if (OperatingSystem.IsWindows())
        {
            // Windows doesn't need shell wrapping for PATH
            ConfigureDirectExecution(startInfo, command);
        }
        else
        {
            // Linux: prefer bash, fall back to sh for minimal distros (e.g., Alpine)
            var shell = File.Exists("/bin/bash") ? "/bin/bash" : "/bin/sh";
            var setupScript = shell == "/bin/bash" ? LinuxSetupScript : ShSetupScript;

            startInfo.FileName = shell;
            startInfo.ArgumentList.Add("-c");

            startInfo.ArgumentList.Add($"{setupScript}{envExports}{BuildPosixCommandLine(command)}");
        }
    }

    /// <summary>
    /// Builds the command line for a POSIX -c string. With <see cref="ShellCommand.ArgumentList"/> the
    /// executor quotes the command and every argument; the legacy string form is passed through as given.
    /// A group command is <c>exec</c>ed, so the group's leader is the command itself rather than the shell.
    /// </summary>
    internal static string BuildPosixCommandLine(ShellCommand command)
    {
        var exec = command.OwnProcessGroup ? "exec " : string.Empty;

        if (command.ArgumentList is { } argumentList)
        {
            var builder = new StringBuilder(exec).Append(ShellQuoting.Quote(command.Command));
            foreach (var argument in argumentList)
            {
                builder.Append(' ').Append(ShellQuoting.Quote(argument));
            }
            return builder.ToString();
        }

        return string.IsNullOrEmpty(command.Arguments)
            ? $"{exec}{command.Command}"
            : $"{exec}{command.Command} {command.Arguments}";
    }

    private static void ConfigureDirectExecution(ProcessStartInfo startInfo, ShellCommand command)
    {
        startInfo.FileName = command.Command;

        if (command.ArgumentList is { } argumentList)
        {
            if (OperatingSystem.IsWindows())
            {
                ValidateBatchFileArguments(command.Command, argumentList);
            }

            foreach (var argument in argumentList)
            {
                startInfo.ArgumentList.Add(argument);
            }
        }
        else
        {
            startInfo.Arguments = command.Arguments;
        }

        // Set environment variables directly on Windows
        if (command.EnvironmentVariables != null)
        {
            foreach (var (key, value) in command.EnvironmentVariables)
            {
                startInfo.Environment[key] = value;
            }
        }
    }

    /// <summary>
    /// Windows runs .cmd and .bat files through cmd.exe, which expands % and ! and treats &amp;, |, ^ and
    /// quotes as syntax even inside the quoting .NET applies (the BatBadBut class, CVE-2024-24576).
    /// Arguments headed for a batch file are therefore limited to characters cmd.exe leaves alone.
    /// </summary>
    internal static void ValidateBatchFileArguments(string command, IReadOnlyList<string> arguments)
    {
        var extension = Path.GetExtension(command);
        if (!extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".bat", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var argument in arguments)
        {
            if (!BatchSafeArgumentRegex().IsMatch(argument))
            {
                throw new NotSupportedException(
                    $"Argument '{argument}' cannot be passed safely to the batch file '{command}' because cmd.exe " +
                    "would interpret some of its characters. Launch the underlying .exe instead of the batch file.");
            }
        }
    }

    [GeneratedRegex(@"^[\p{L}\p{N} _\-.,/:=@+\\~]*\z")]
    private static partial Regex BatchSafeArgumentRegex();

    // Valid POSIX environment variable name
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex ValidEnvKeyRegex();

    private string BuildEnvironmentExports(IReadOnlyDictionary<string, string>? environmentVariables)
    {
        if (environmentVariables == null || environmentVariables.Count == 0)
        {
            return string.Empty;
        }

        var exports = new StringBuilder();
        foreach (var (key, value) in environmentVariables)
        {
            // Validate key to prevent shell injection via malformed variable names
            if (!ValidEnvKeyRegex().IsMatch(key))
            {
                _logger?.LogWarning(
                    "Skipping invalid environment variable name: '{Key}'. " +
                    "Names must start with a letter or underscore and contain only letters, digits, and underscores.",
                    key);
                continue;
            }

            // Escape single quotes in values for shell safety
            var escapedValue = value.Replace("'", "'\\''");
            exports.Append($"export {key}='{escapedValue}'; ");
        }

        return exports.ToString();
    }
}
