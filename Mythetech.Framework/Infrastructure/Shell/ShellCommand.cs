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

    private readonly string _arguments = string.Empty;
    private readonly IReadOnlyList<string>? _argumentList;

    /// <summary>
    /// Arguments to pass to the command as a single pre-quoted string.
    /// Use <see cref="ShellQuoting"/> to escape user-supplied values.
    /// Prefer <see cref="ArgumentList"/>, which needs no quoting.
    /// Cannot be combined with <see cref="ArgumentList"/>.
    /// </summary>
    public string Arguments
    {
        get => _arguments;
        init
        {
            if (!string.IsNullOrEmpty(value) && _argumentList is not null)
                throw new ArgumentException($"{nameof(Arguments)} cannot be set when {nameof(ArgumentList)} is set.", nameof(Arguments));
            _arguments = value;
        }
    }

    /// <summary>
    /// Arguments to pass to the command, one value per argument, exactly as the child process should receive them.
    /// The executor quotes each value (and <see cref="Command"/>) for the platform, so callers never quote.
    /// Cannot be combined with a non-empty <see cref="Arguments"/>.
    /// </summary>
    /// <remarks>
    /// On Windows, batch files (.cmd, .bat) run through cmd.exe, so the desktop executor refuses values
    /// containing characters cmd.exe would interpret. Launch the .exe directly where one exists.
    /// </remarks>
    public IReadOnlyList<string>? ArgumentList
    {
        get => _argumentList;
        init
        {
            if (value is not null && !string.IsNullOrEmpty(_arguments))
                throw new ArgumentException($"{nameof(ArgumentList)} cannot be set when {nameof(Arguments)} is set.", nameof(ArgumentList));
            _argumentList = value;
        }
    }

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
    /// Creates a new command with the specified argument list. Values are passed verbatim; do not quote them.
    /// </summary>
    public ShellCommand WithArgumentList(params IEnumerable<string> args) => this with { ArgumentList = [.. args] };

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
