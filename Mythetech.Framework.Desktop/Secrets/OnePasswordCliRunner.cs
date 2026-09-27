using System.Diagnostics;
using System.Text;

namespace Mythetech.Framework.Desktop.Secrets;

/// <summary>
/// Starts the 1Password CLI as a child process.
/// </summary>
internal sealed class OnePasswordCliRunner : IOnePasswordCliRunner
{
    // op reads and writes UTF-8. Setting it explicitly keeps non-ASCII secrets intact on Windows, where the
    // default redirect encoding follows the console code page, and keeps a BOM out of the piped JSON.
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _executable;

    /// <summary>
    /// Creates a runner for the given executable.
    /// </summary>
    /// <param name="executable">The CLI to start. Defaults to "op" resolved from PATH.</param>
    public OnePasswordCliRunner(string executable = "op")
    {
        _executable = executable;
    }

    /// <inheritdoc />
    public async Task<OnePasswordCliResult> RunAsync(
        IReadOnlyList<string> arguments,
        string? standardInput,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput != null,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (standardInput != null)
        {
            startInfo.StandardInputEncoding = Utf8NoBom;
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            if (standardInput != null)
            {
                await WriteStandardInputAsync(process, standardInput, cancellationToken);
            }

            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
                // Ignore errors when killing the process
            }
            throw;
        }

        return new OnePasswordCliResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
    }

    private static async Task WriteStandardInputAsync(Process process, string standardInput, CancellationToken cancellationToken)
    {
        try
        {
            await process.StandardInput.WriteAsync(standardInput.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
        }
        catch (IOException)
        {
            // op can exit before reading its input (for example when 1Password is locked). The broken pipe is
            // not the failure worth reporting; the exit code and stderr collected after it are.
        }
        finally
        {
            try
            {
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // Closing a pipe the child already closed fails the same way; the result still comes from exit.
            }
        }
    }
}
