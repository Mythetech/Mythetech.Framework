#if NET11_0_OR_GREATER
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

/// <summary>
/// Drives a <see cref="ProcessGroupLifeline"/> against real groups. Disposing it closes the pipe exactly as the system
/// does when the app's process ends, so it stands in for a crash.
/// </summary>
[UnsupportedOSPlatform("windows")]
public class ProcessGroupLifelineTests
{
    private static readonly TimeSpan GoneTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SurvivesTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly DateTimeOffset AnotherStartTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ShellExecutor _executor = new();

    private static ShellCommand SleepCommand =>
        new ShellCommand { Command = "/bin/sleep" }.WithArgumentList("30");

    private static ShellCommand SleepGroupCommand => SleepCommand.WithOwnProcessGroup();

    // The leader exits once it reads a line, leaving its child in the group with no leader to reach it through.
    private static ShellCommand LeaderExitsLeavingAChildCommand =>
        new ShellCommand { Command = "/bin/sh" }.WithArgumentList("-c", "sleep 30 & echo $!; read line; exit 0").WithOwnProcessGroup();

    [Fact(DisplayName = "Dispose_KillsEveryGroupStillListed")]
    public async Task Dispose_KillsEveryGroupStillListed()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var first = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        await using var second = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        var lifeline = new ProcessGroupLifeline();
        lifeline.Adopt(first.ProcessId, first.StartedAt);
        lifeline.Adopt(second.ProcessId, second.StartedAt);

        lifeline.Dispose();

        (await WhenTrueAsync(() => ProcessGroups.IsEmpty(first.ProcessId) && ProcessGroups.IsEmpty(second.ProcessId), GoneTimeout))
            .ShouldBeTrue();
    }

    [Fact(DisplayName = "Dispose_KillsWhatALeaderThatExitedLeftInItsGroup")]
    public async Task Dispose_KillsWhatALeaderThatExitedLeftInItsGroup()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(LeaderExitsLeavingAChildCommand, TestContext.Current.CancellationToken);
        var lifeline = new ProcessGroupLifeline();
        lifeline.Adopt(process.ProcessId, process.StartedAt);
        var childPid = await FirstPidPrintedAsync(process);
        await process.WriteInputAsync("\n", TestContext.Current.CancellationToken);
        (await WhenTrueAsync(() => process.HasExited, GoneTimeout)).ShouldBeTrue();

        lifeline.Dispose();

        (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
    }

    [Fact(DisplayName = "Dispose_LeavesAReleasedGroupRunning")]
    public async Task Dispose_LeavesAReleasedGroupRunning()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var released = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        await using var listed = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        var lifeline = new ProcessGroupLifeline();
        lifeline.Adopt(released.ProcessId, released.StartedAt);
        lifeline.Adopt(listed.ProcessId, listed.StartedAt);

        lifeline.Release(released.ProcessId);
        lifeline.Dispose();

        (await WhenTrueAsync(() => ProcessGroups.IsEmpty(listed.ProcessId), GoneTimeout)).ShouldBeTrue();
        (await WhenTrueAsync(() => released.HasExited, SurvivesTimeout)).ShouldBeFalse();
        lifeline.Holds(released.ProcessId).ShouldBeFalse();
    }

    [Fact(DisplayName = "Dispose_ChildStartedAfterTheWatchdog_DoesNotHoldThePipeOpen")]
    public async Task Dispose_ChildStartedAfterTheWatchdog_DoesNotHoldThePipeOpen()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var group = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        var lifeline = new ProcessGroupLifeline();
        lifeline.Adopt(group.ProcessId, group.StartedAt);

        // Had it inherited the app's end of the pipe, the watchdog would see no end of input while it runs.
        await using var bystander = await _executor.StartProcessAsync(SleepCommand, TestContext.Current.CancellationToken);
        lifeline.Dispose();

        (await WhenTrueAsync(() => ProcessGroups.IsEmpty(group.ProcessId), GoneTimeout)).ShouldBeTrue();
        bystander.HasExited.ShouldBeFalse();
    }

    [Fact(DisplayName = "Watchdog_RunsOutsideTheAppsSessionAndTree")]
    public async Task Watchdog_RunsOutsideTheAppsSessionAndTree()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var group = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        using var lifeline = new ProcessGroupLifeline();
        lifeline.Adopt(group.ProcessId, group.StartedAt);

        var watchdog = lifeline.WatchdogProcessId.ShouldNotBeNull();

        getsid(watchdog).ShouldNotBe(getsid(0));
        ProcessGroups.GroupOf(watchdog).ShouldNotBe(ProcessGroups.GroupOf(Environment.ProcessId));
        (await WhenTrueAsync(() => ParentOf(watchdog) != Environment.ProcessId, GoneTimeout)).ShouldBeTrue();
    }

    [Fact(DisplayName = "Adopt_AfterTheWatchdogWasKilled_StartsAnotherThatListsEveryGroup")]
    public async Task Adopt_AfterTheWatchdogWasKilled_StartsAnotherThatListsEveryGroup()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var first = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        await using var second = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        var lifeline = new ProcessGroupLifeline();
        lifeline.Adopt(first.ProcessId, first.StartedAt);
        var killed = lifeline.WatchdogProcessId.ShouldNotBeNull();
        ProcessGroups.SignalProcess(killed, ProcessGroups.SIGKILL).ShouldBeTrue();
        (await WhenTrueAsync(() => IsGone(killed), GoneTimeout)).ShouldBeTrue();

        lifeline.Adopt(second.ProcessId, second.StartedAt);

        lifeline.WatchdogProcessId.ShouldNotBeNull().ShouldNotBe(killed);
        first.HasExited.ShouldBeFalse();
        lifeline.Dispose();
        (await WhenTrueAsync(() => ProcessGroups.IsEmpty(first.ProcessId) && ProcessGroups.IsEmpty(second.ProcessId), GoneTimeout))
            .ShouldBeTrue();
    }

    [Fact(DisplayName = "Adopt_TheAppsOwnGroup_IsNotListed")]
    public void Adopt_TheAppsOwnGroup_IsNotListed()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        using var lifeline = new ProcessGroupLifeline();
        var ownGroup = ProcessGroups.GroupOf(Environment.ProcessId);

        lifeline.Adopt(ownGroup, DateTimeOffset.MinValue);

        lifeline.Holds(ownGroup).ShouldBeFalse();
        lifeline.WatchdogProcessId.ShouldBeNull();
    }

    [Fact(DisplayName = "Adopt_AProcessThatSharesTheAppsGroup_IsNotListed")]
    public async Task Adopt_AProcessThatSharesTheAppsGroup_IsNotListed()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(SleepCommand, TestContext.Current.CancellationToken);
        using var lifeline = new ProcessGroupLifeline();

        lifeline.Adopt(process.ProcessId, process.StartedAt);

        lifeline.Holds(process.ProcessId).ShouldBeFalse();
        lifeline.WatchdogProcessId.ShouldBeNull();
    }

    [Fact(DisplayName = "Prune_StrikesOffAGroupWithNoProcessLeft")]
    public async Task Prune_StrikesOffAGroupWithNoProcessLeft()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var ended = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        await using var running = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        using var lifeline = new ProcessGroupLifeline();
        lifeline.Adopt(ended.ProcessId, ended.StartedAt);
        lifeline.Adopt(running.ProcessId, running.StartedAt);
        ended.Kill();
        (await WhenTrueAsync(() => ProcessGroups.IsEmpty(ended.ProcessId), GoneTimeout)).ShouldBeTrue();

        lifeline.Prune();

        lifeline.Holds(ended.ProcessId).ShouldBeFalse();
        lifeline.Holds(running.ProcessId).ShouldBeTrue();
    }

    [Fact(DisplayName = "Prune_StrikesOffAGroupWhoseLeaderPidWasReused")]
    public async Task Prune_StrikesOffAGroupWhoseLeaderPidWasReused()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        using var lifeline = new ProcessGroupLifeline(startTimeOf: _ => AnotherStartTime);
        lifeline.Adopt(process.ProcessId, process.StartedAt);

        lifeline.Prune();

        lifeline.Holds(process.ProcessId).ShouldBeFalse();
    }

    [Fact(DisplayName = "StartProcess_OwnProcessGroup_IsListedUntilDisposed")]
    public async Task StartProcess_OwnProcessGroup_IsListedUntilDisposed()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var process = await _executor.StartProcessAsync(SleepGroupCommand, TestContext.Current.CancellationToken);
        var listed = ProcessGroupLifeline.Shared.Holds(process.ProcessId);

        await process.DisposeAsync();

        listed.ShouldBeTrue();
        ProcessGroupLifeline.Shared.Holds(process.ProcessId).ShouldBeFalse();
    }

    private static async Task<int> FirstPidPrintedAsync(IShellProcess process)
    {
        var line = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.OutputReceived += text => line.TrySetResult(text);
        var printed = await line.Task.WaitAsync(GoneTimeout, TestContext.Current.CancellationToken);
        return int.Parse(printed.Trim(), CultureInfo.InvariantCulture);
    }

    private static bool IsGone(int pid) =>
        !ProcessGroups.SignalProcess(pid, 0) && Marshal.GetLastPInvokeError() == 3;

    private static int ParentOf(int pid)
    {
        using var ps = Process.Start(new ProcessStartInfo("ps", ["-o", "ppid=", "-p", pid.ToString(CultureInfo.InvariantCulture)])
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        })!;
        var output = ps.StandardOutput.ReadToEnd();
        ps.WaitForExit();
        return int.Parse(output.Trim(), CultureInfo.InvariantCulture);
    }

    private static async Task<bool> WhenTrueAsync(Func<bool> condition, TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed >= timeout)
                return false;
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        return true;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int getsid(int pid);
}
#endif
