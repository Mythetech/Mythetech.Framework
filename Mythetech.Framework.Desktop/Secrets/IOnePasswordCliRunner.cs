namespace Mythetech.Framework.Desktop.Secrets;

/// <summary>
/// Runs the 1Password CLI. Kept behind an interface so tests can check exactly what reaches argv and stdin
/// without starting the real CLI.
/// </summary>
internal interface IOnePasswordCliRunner
{
    /// <summary>
    /// Runs the CLI with the given arguments and waits for it to exit.
    /// </summary>
    /// <param name="arguments">Arguments passed one value per argument, never joined into a command line.</param>
    /// <param name="standardInput">
    /// Text written to the process's stdin, which is then closed. Secret values go here, never in
    /// <paramref name="arguments"/>, because other processes can read a process's arguments. When null, stdin
    /// is not redirected, since some commands switch to reading input when stdin is a pipe.
    /// </param>
    /// <param name="cancellationToken">Cancels the wait and kills the process.</param>
    Task<OnePasswordCliResult> RunAsync(
        IReadOnlyList<string> arguments,
        string? standardInput,
        CancellationToken cancellationToken);
}

/// <summary>
/// Exit code and captured output of one 1Password CLI run.
/// </summary>
internal sealed record OnePasswordCliResult(int ExitCode, string StandardOutput, string StandardError);
