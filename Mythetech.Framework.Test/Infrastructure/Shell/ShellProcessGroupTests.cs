using System.Diagnostics;
using System.Globalization;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

/// <summary>
/// Starts real processes through <see cref="ShellExecutor"/> and checks the process group each lands in.
/// </summary>
public class ShellProcessGroupTests
{
    private static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(10);

    private readonly ShellExecutor _executor = new();

    private static ShellCommand BackgroundChildCommand =>
        new ShellCommand { Command = "/bin/sh" }.WithArgumentList("-c", "sleep 30 & echo $!; wait");

    private static ShellCommand SleepCommand =>
        new ShellCommand { Command = "/bin/sleep" }.WithArgumentList("30");

    [Fact(DisplayName = "StartProcess_OwnProcessGroup_LeadsItsGroup")]
    public async Task StartProcess_OwnProcessGroup_LeadsItsGroup()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(
            BackgroundChildCommand.WithOwnProcessGroup(), TestContext.Current.CancellationToken);
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            var childPid = await backgroundPid;

            ProcessGroups.GroupOf(process.ProcessId).ShouldBe(process.ProcessId);
            ProcessGroups.GroupOf(childPid).ShouldBe(process.ProcessId);
            process.OwnsProcessGroup.ShouldBeTrue();
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StartProcess_WithoutOwnProcessGroup_SharesTheCallersGroup")]
    public async Task StartProcess_WithoutOwnProcessGroup_SharesTheCallersGroup()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(
            BackgroundChildCommand, TestContext.Current.CancellationToken);
        try
        {
            ProcessGroups.GroupOf(process.ProcessId).ShouldNotBe(process.ProcessId);
            ProcessGroups.GroupOf(process.ProcessId).ShouldBe(ProcessGroups.GroupOf(Environment.ProcessId));
            process.OwnsProcessGroup.ShouldBeFalse();
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StartProcess_OwnProcessGroup_ReportsTheCommandNotTheShell")]
    public async Task StartProcess_OwnProcessGroup_ReportsTheCommandNotTheShell()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(
            SleepCommand.WithOwnProcessGroup(), TestContext.Current.CancellationToken);
        try
        {
            var name = await CommandNameOnceSettledAsync(process.ProcessId, "sleep");

            name.ShouldEndWith("sleep");
        }
        finally
        {
            process.Kill();
        }
    }

    // bash 5.1+ and zsh exec the last command of a -c string anyway unless an rc file sets an EXIT trap, so check the line.
    [Fact(DisplayName = "BuildPosixCommandLine_OwnProcessGroup_ExecsTheCommand")]
    public void BuildPosixCommandLine_OwnProcessGroup_ExecsTheCommand()
    {
        ShellExecutor.BuildPosixCommandLine(SleepCommand.WithOwnProcessGroup()).ShouldBe("exec '/bin/sleep' '30'");
        ShellExecutor.BuildPosixCommandLine(new ShellCommand { Command = "claude", Arguments = "--print hi" }.WithOwnProcessGroup())
            .ShouldBe("exec claude --print hi");
        ShellExecutor.BuildPosixCommandLine(new ShellCommand { Command = "claude" }.WithOwnProcessGroup())
            .ShouldBe("exec claude");
    }

    [Fact(DisplayName = "BuildPosixCommandLine_WithoutOwnProcessGroup_RunsTheCommandAsGiven")]
    public void BuildPosixCommandLine_WithoutOwnProcessGroup_RunsTheCommandAsGiven()
    {
        ShellExecutor.BuildPosixCommandLine(SleepCommand).ShouldBe("'/bin/sleep' '30'");
        ShellExecutor.BuildPosixCommandLine(new ShellCommand { Command = "claude", Arguments = "--print hi" })
            .ShouldBe("claude --print hi");
    }

    [Fact(DisplayName = "StartProcess_StartedAt_MatchesTheOs")]
    public async Task StartProcess_StartedAt_MatchesTheOs()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(SleepCommand, TestContext.Current.CancellationToken);
        try
        {
            using var osProcess = Process.GetProcessById(process.ProcessId);
            DateTimeOffset osStartedAt = osProcess.StartTime.ToUniversalTime();

            (process.StartedAt - osStartedAt).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));
            process.StartedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StartProcess_StartedAt_KnownAfterTheProcessExits")]
    public async Task StartProcess_StartedAt_KnownAfterTheProcessExits()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var beforeStart = DateTimeOffset.UtcNow;
        await using var process = await _executor.StartProcessAsync(SleepCommand, TestContext.Current.CancellationToken);

        process.Kill();
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        process.StartedAt.ShouldBeInRange(beforeStart - TimeSpan.FromSeconds(1), DateTimeOffset.UtcNow);
    }

    [Fact(DisplayName = "IsEmpty_GroupWithAMember_FalseUntilTheGroupExits")]
    public async Task IsEmpty_GroupWithAMember_FalseUntilTheGroupExits()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(
            BackgroundChildCommand.WithOwnProcessGroup(), TestContext.Current.CancellationToken);
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            await backgroundPid;

            ProcessGroups.IsEmpty(process.ProcessId).ShouldBeFalse();
        }
        finally
        {
            process.Kill();
        }

        (await WhenTrueAsync(() => ProcessGroups.IsEmpty(process.ProcessId))).ShouldBeTrue();
    }

    private static Task<int> FirstPidPrinted(IShellProcess process)
    {
        var pid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.OutputReceived += line =>
        {
            if (int.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                pid.TrySetResult(value);
        };
        return pid.Task.WaitAsync(SettleTimeout, TestContext.Current.CancellationToken);
    }

    // The login shell runs its setup script before it reaches the command, so the name changes once it does.
    private async Task<string> CommandNameOnceSettledAsync(int pid, string expectedSuffix)
    {
        var command = new ShellCommand { Command = "/bin/ps" }
            .WithArgumentList("-o", "comm=", "-p", pid.ToString(CultureInfo.InvariantCulture))
            .WithoutShell();
        var name = string.Empty;
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < SettleTimeout)
        {
            var result = await _executor.ExecuteAsync(command, TestContext.Current.CancellationToken);
            name = result.StandardOutput.Trim();
            if (name.EndsWith(expectedSuffix, StringComparison.Ordinal))
                break;
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        return name;
    }

    private static async Task<bool> WhenTrueAsync(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed > SettleTimeout)
                return false;
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        return true;
    }
}
