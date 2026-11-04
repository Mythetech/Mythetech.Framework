using System.Diagnostics;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

/// <summary>
/// Runs real shell commands against a temporary home whose profile counts how often it is sourced.
/// </summary>
public sealed class ShellExecutorLoginEnvironmentTests : IDisposable
{
    private const string ProbeProfile =
        "echo sourced >> \"$HOME/sourced\"\n" +
        "export MT_LOGIN_PROBE=from-profile\n";

    private static readonly ShellCommand PrintProbe = new ShellCommand { Command = "printenv" }.WithArgumentList("MT_LOGIN_PROBE");

    private readonly DirectoryInfo _home = Directory.CreateTempSubdirectory("login-env-home");

    private int TimesSourced => File.Exists(Path.Combine(_home.FullName, "sourced"))
        ? File.ReadAllLines(Path.Combine(_home.FullName, "sourced")).Length
        : 0;

    [Fact(DisplayName = "UseShell_ProfileExportsVariable_CommandSeesIt")]
    public async Task UseShell_ProfileExportsVariable_CommandSeesIt()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows runs commands without a login shell");
        var executor = ExecutorWithProfile(ProbeProfile);

        var result = await executor.ExecuteAsync(PrintProbe, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.ShouldBe("from-profile");
    }

    [Fact(DisplayName = "UseShell_ConcurrentAndLaterCommands_ProfileSourcedOnce")]
    public async Task UseShell_ConcurrentAndLaterCommands_ProfileSourcedOnce()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows runs commands without a login shell");
        var executor = ExecutorWithProfile(ProbeProfile);
        var ct = TestContext.Current.CancellationToken;

        var concurrent = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => executor.ExecuteAsync(PrintProbe, ct)));
        var later = await executor.ExecuteAsync(PrintProbe, ct);

        concurrent.Append(later).ShouldAllBe(r => r.ExitCode == 0);
        TimesSourced.ShouldBe(1);
    }

    [Fact(DisplayName = "UseShell_CommandSetsSameVariable_CommandValueWins")]
    public async Task UseShell_CommandSetsSameVariable_CommandValueWins()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows runs commands without a login shell");
        var executor = ExecutorWithProfile(ProbeProfile);
        var command = PrintProbe.WithEnvironment(new Dictionary<string, string> { ["MT_LOGIN_PROBE"] = "from-command" });

        var result = await executor.ExecuteAsync(command, TestContext.Current.CancellationToken);

        result.StandardOutput.ShouldBe("from-command");
    }

    [Fact(DisplayName = "StartProcessAsync_UseShell_ProcessSeesProfileVariables")]
    public async Task StartProcessAsync_UseShell_ProcessSeesProfileVariables()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows runs commands without a login shell");
        var executor = ExecutorWithProfile(ProbeProfile);
        var ct = TestContext.Current.CancellationToken;
        var command = new ShellCommand { Command = "sh" }.WithArgumentList("-c", "read line; printenv MT_LOGIN_PROBE");

        await using var process = await executor.StartProcessAsync(command, ct);
        var output = new List<string>();
        process.OutputReceived += output.Add;
        await process.WriteInputAsync("go\n", ct);
        await process.WaitForExitAsync(ct);

        output.ShouldBe(["from-profile"]);
    }

    [Fact(DisplayName = "WithoutShell_Command_ProfileNeverSourced")]
    public async Task WithoutShell_Command_ProfileNeverSourced()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows runs commands without a login shell");
        var executor = ExecutorWithProfile(ProbeProfile);

        await executor.ExecuteAsync(PrintProbe.WithoutShell(), TestContext.Current.CancellationToken);

        TimesSourced.ShouldBe(0);
    }

    [Fact(DisplayName = "UseShell_ProfileExitsEarly_NotRetriedForEachCommand")]
    public async Task UseShell_ProfileExitsEarly_NotRetriedForEachCommand()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows runs commands without a login shell");
        var executor = ExecutorWithProfile("echo sourced >> \"$HOME/sourced\"\nexit 3\n");
        var ct = TestContext.Current.CancellationToken;
        var echo = new ShellCommand { Command = "echo" }.WithArgumentList("ok");

        var first = await executor.ExecuteAsync(echo, ct);
        var second = await executor.ExecuteAsync(echo, ct);

        first.StandardOutput.ShouldBe("ok");
        second.StandardOutput.ShouldBe("ok");
        TimesSourced.ShouldBe(1);
    }

    [Fact(DisplayName = "UseShell_ProfileHangs_CommandRunsAfterTimeout")]
    public async Task UseShell_ProfileHangs_CommandRunsAfterTimeout()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows runs commands without a login shell");
        var executor = ExecutorWithProfile("sleep 30\n", timeout: TimeSpan.FromMilliseconds(500));
        var echo = new ShellCommand { Command = "echo" }.WithArgumentList("ok");
        var stopwatch = Stopwatch.StartNew();

        var result = await executor.ExecuteAsync(echo, TestContext.Current.CancellationToken);

        result.StandardOutput.ShouldBe("ok");
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
    }

    private ShellExecutor ExecutorWithProfile(string profile, TimeSpan? timeout = null)
    {
        File.WriteAllText(Path.Combine(_home.FullName, ".zshrc"), profile);
        File.WriteAllText(Path.Combine(_home.FullName, ".bashrc"), profile);

        var loginEnvironment = new LoginEnvironment(
            timeout ?? TimeSpan.FromSeconds(30),
            new Dictionary<string, string> { ["HOME"] = _home.FullName });

        return new ShellExecutor(loginEnvironment);
    }

    public void Dispose() => _home.Delete(recursive: true);
}
