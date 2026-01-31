namespace Mythetech.Framework.Infrastructure.Shell;

/// <summary>
/// Provides cross-platform shell command execution with proper PATH setup
/// for desktop GUI applications.
/// </summary>
public interface IShellExecutor
{
    /// <summary>
    /// Executes a command and returns the result after completion.
    /// Best for simple commands where you just need the output.
    /// </summary>
    Task<ShellResult> ExecuteAsync(
        ShellCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command with streaming output callbacks.
    /// Best for long-running processes where you need real-time output.
    /// </summary>
    Task<ShellResult> ExecuteStreamingAsync(
        ShellCommand command,
        Action<string>? onStdOut = null,
        Action<string>? onStdErr = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an interactive process with stdin access.
    /// Returns a handle for writing to stdin and reading output.
    /// </summary>
    Task<IShellProcess> StartProcessAsync(
        ShellCommand command,
        CancellationToken cancellationToken = default);
}
