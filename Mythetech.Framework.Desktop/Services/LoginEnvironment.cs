using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// The variables the user's login shell profile sets, worked out once per process so a shell command starts with
/// them instead of sourcing the profile again.
/// </summary>
internal sealed class LoginEnvironment
{
    // Each shell sets these for itself, so a copied value would be stale.
    private static readonly HashSet<string> ShellOwned = ["PWD", "OLDPWD", "SHLVL", "_"];

    private readonly TimeSpan _timeout;
    private readonly IReadOnlyDictionary<string, string>? _overrides;
    private readonly Lock _lock = new();
    private Task<IReadOnlyDictionary<string, string>?>? _resolution;

    internal static LoginEnvironment Shared { get; } = new(TimeSpan.FromSeconds(10));

    /// <param name="timeout">How long the profile may take before each command falls back to sourcing it.</param>
    /// <param name="overrides">Variables the login shell starts with on top of the app's own.</param>
    internal LoginEnvironment(TimeSpan timeout, IReadOnlyDictionary<string, string>? overrides = null)
    {
        _timeout = timeout;
        _overrides = overrides;
    }

    /// <summary>
    /// The variables to add to a shell command's environment, or null when the profile could not be read, in which
    /// case each command sources it itself. The first call starts the work; every call shares its result.
    /// </summary>
    internal Task<IReadOnlyDictionary<string, string>?> GetAsync(ILogger? logger, CancellationToken cancellationToken)
    {
        Task<IReadOnlyDictionary<string, string>?> resolution;
        lock (_lock)
        {
            resolution = _resolution ??= ResolveAsync(logger);
        }

        return resolution.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Reads the NUL-separated <c>env -0</c> output between the first two markers, ignoring anything the profile
    /// printed around it. Returns null when there is no environment between them.
    /// </summary>
    internal static IReadOnlyDictionary<string, string>? Parse(string output, string marker)
    {
        var start = output.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        var end = output.IndexOf(marker, start, StringComparison.Ordinal);
        if (end < 0)
        {
            return null;
        }

        var environment = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in output[start..end].Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = entry.IndexOf('=');
            if (separator > 0)
            {
                environment[entry[..separator]] = entry[(separator + 1)..];
            }
        }

        return environment.Count == 0 ? null : environment;
    }

    /// <summary>
    /// The variables the profile added or changed relative to the environment the login shell started with.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> Changes(
        IReadOnlyDictionary<string, string> login,
        IReadOnlyDictionary<string, string> inherited)
    {
        var changes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in login)
        {
            if (ShellOwned.Contains(key) || (inherited.TryGetValue(key, out var before) && before == value))
            {
                continue;
            }

            changes[key] = value;
        }

        return changes;
    }

    private async Task<IReadOnlyDictionary<string, string>?> ResolveAsync(ILogger? logger)
    {
        var (shell, setupScript) = ShellExecutor.LoginShell();
        var marker = $"__MT_LOGIN_ENV_{Guid.NewGuid():N}__";

        var startInfo = new ProcessStartInfo(shell)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add($"{setupScript}printf '%s' '{marker}'; env -0; printf '%s' '{marker}'");

        foreach (var (key, value) in _overrides ?? new Dictionary<string, string>())
        {
            startInfo.Environment[key] = value;
        }

        var inherited = startInfo.Environment
            .Where(entry => entry.Value is not null)
            .ToDictionary(entry => entry.Key, entry => entry.Value!, StringComparer.Ordinal);

        var stopwatch = Stopwatch.StartNew();
        using var process = new Process { StartInfo = startInfo };
        using var timeout = new CancellationTokenSource(_timeout);
        try
        {
            process.Start();
            await using var killOnTimeout = timeout.Token.Register(() => Kill(process));
            process.StandardInput.Close();

            var output = await ReadThroughEndMarkerAsync(process.StandardOutput, marker, timeout.Token);
            var login = Parse(output, marker);
            if (login is null)
            {
                await process.WaitForExitAsync(timeout.Token);
                logger?.LogWarning(
                    "The login shell exited with code {ExitCode} before printing its environment, so each shell command sources the profile itself",
                    process.ExitCode);
                return null;
            }

            logger?.LogDebug("Read the login shell environment in {Elapsed} ms", stopwatch.ElapsedMilliseconds);
            return Changes(login, inherited);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            logger?.LogWarning(
                "The login shell did not print its environment within {Timeout}, so each shell command sources the profile itself",
                _timeout);
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "The login shell could not be read, so each shell command sources the profile itself");
            return null;
        }
    }

    // Stops at the end marker rather than end of file, since something the profile started can keep stdout open.
    private static async Task<string> ReadThroughEndMarkerAsync(StreamReader reader, string marker, CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        var buffer = new char[8192];
        int read;
        while ((read = await reader.ReadAsync(buffer, cancellationToken)) > 0)
        {
            output.Append(buffer, 0, read);
            if (Parse(output.ToString(), marker) is not null)
            {
                break;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return output.ToString();
    }

    private static void Kill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // It has already exited.
        }
    }
}
