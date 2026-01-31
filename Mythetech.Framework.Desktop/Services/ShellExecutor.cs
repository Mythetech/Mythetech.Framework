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
    public async Task<ShellResult> ExecuteStreamingAsync(
        ShellCommand command,
        Action<string>? onStdOut = null,
        Action<string>? onStdErr = null,
        CancellationToken cancellationToken = default)
    {
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

        using var timeoutCts = command.Timeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;

        if (command.Timeout.HasValue && timeoutCts != null)
        {
            timeoutCts.CancelAfter(command.Timeout.Value);
        }

        var effectiveCt = timeoutCts?.Token ?? cancellationToken;

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

    /// <inheritdoc />
    public Task<IShellProcess> StartProcessAsync(
        ShellCommand command,
        CancellationToken cancellationToken = default)
    {
        var startInfo = CreateProcessStartInfo(command, redirectInput: true);
        var shellProcess = new ShellProcess(startInfo, _logger);
        shellProcess.Start();
        return Task.FromResult<IShellProcess>(shellProcess);
    }

    private ProcessStartInfo CreateProcessStartInfo(ShellCommand command, bool redirectInput = false)
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

            var fullCommand = string.IsNullOrEmpty(command.Arguments)
                ? $"{MacOsSetupScript}{envExports}{command.Command}"
                : $"{MacOsSetupScript}{envExports}{command.Command} {command.Arguments}";

            startInfo.ArgumentList.Add(fullCommand);
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

            var fullCommand = string.IsNullOrEmpty(command.Arguments)
                ? $"{setupScript}{envExports}{command.Command}"
                : $"{setupScript}{envExports}{command.Command} {command.Arguments}";

            startInfo.ArgumentList.Add(fullCommand);
        }
    }

    private static void ConfigureDirectExecution(ProcessStartInfo startInfo, ShellCommand command)
    {
        startInfo.FileName = command.Command;
        startInfo.Arguments = command.Arguments;

        // Set environment variables directly on Windows
        if (command.EnvironmentVariables != null)
        {
            foreach (var (key, value) in command.EnvironmentVariables)
            {
                startInfo.Environment[key] = value;
            }
        }
    }

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
