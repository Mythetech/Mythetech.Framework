namespace Mythetech.Framework.Infrastructure.Shell;

/// <summary>
/// Command specification for shell execution.
/// Immutable record with builder pattern for fluent API.
/// </summary>
public record ShellCommand
{
    /// <summary>
    /// The command or executable to run (e.g., "git", "dotnet", "npm").
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Arguments to pass to the command.
    /// Use <see cref="ShellQuoting"/> to escape user-supplied values.
    /// </summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    /// Working directory for command execution.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Environment variables to set for the process.
    /// </summary>
    public IReadOnlyDictionary<string, string>? EnvironmentVariables { get; init; }

    /// <summary>
    /// Whether to wrap in platform shell for PATH resolution.
    /// <list type="bullet">
    /// <item>macOS: /bin/zsh -c with path_helper, brew shellenv, profile sourcing</item>
    /// <item>Linux: /bin/bash -c with profile sourcing</item>
    /// <item>Windows: Direct execution (shell wrapping provides no benefit)</item>
    /// </list>
    /// Default: true.
    /// </summary>
    public bool UseShell { get; init; } = true;

    /// <summary>
    /// Timeout for command execution. Default: no timeout.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Creates a new command with the specified arguments.
    /// </summary>
    public ShellCommand WithArguments(string args) => this with { Arguments = args };

    /// <summary>
    /// Creates a new command with the specified working directory.
    /// </summary>
    public ShellCommand WithWorkingDirectory(string dir) => this with { WorkingDirectory = dir };

    /// <summary>
    /// Creates a new command with the specified environment variables.
    /// </summary>
    public ShellCommand WithEnvironment(IReadOnlyDictionary<string, string> env) =>
        this with { EnvironmentVariables = env };

    /// <summary>
    /// Creates a new command with the specified timeout.
    /// </summary>
    public ShellCommand WithTimeout(TimeSpan timeout) => this with { Timeout = timeout };

    /// <summary>
    /// Creates a new command with shell wrapping disabled.
    /// Use when the command is already a full path or shell features aren't needed.
    /// </summary>
    public ShellCommand WithoutShell() => this with { UseShell = false };
}
