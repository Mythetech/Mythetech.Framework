namespace Mythetech.Framework.Infrastructure.Shell;

/// <summary>
/// Result of a shell command execution.
/// </summary>
public record ShellResult
{
    /// <summary>
    /// The process exit code.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Whether the command succeeded (exit code 0).
    /// </summary>
    public bool Success => ExitCode == 0;

    /// <summary>
    /// The standard output from the process.
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;

    /// <summary>
    /// The standard error from the process.
    /// </summary>
    public string StandardError { get; init; } = string.Empty;

    /// <summary>
    /// How long the command took to execute.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// When the process started.
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// When the process exited.
    /// </summary>
    public DateTimeOffset ExitTime { get; init; }
}
