using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

/// <summary>
/// Starts real processes through <see cref="ShellExecutor"/> and checks the process group each lands in.
/// </summary>
[UnsupportedOSPlatform("windows")]
public class ShellProcessGroupTests
{
    private const string PerlPath = "/usr/bin/perl";

    private static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan GoneTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SurvivesTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly DateTimeOffset AnotherStartTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ShellExecutor _executor = new();

    private static ShellCommand BackgroundChildCommand =>
        new ShellCommand { Command = "/bin/sh" }.WithArgumentList("-c", "sleep 30 & echo $!; wait");

    private static ShellCommand SleepCommand =>
        new ShellCommand { Command = "/bin/sleep" }.WithArgumentList("30");

    // The subshell exits once it has printed, which orphans the sleep: the leader's tree no longer reaches it, its group does.
    private static ShellCommand OrphanedChildCommand =>
        new ShellCommand { Command = "/bin/sh" }.WithArgumentList("-c", "(sleep 60 & echo $!); sleep 60").WithOwnProcessGroup();

    // The leader exits at once and leaves the sleep in its group, so only the pid-reuse check decides whether it is signalled.
    private static ShellCommand LeaderExitsLeavingAChildCommand =>
        new ShellCommand { Command = "/bin/sh" }.WithArgumentList("-c", "sleep 60 & echo $!; exit 0").WithOwnProcessGroup();

    // The leader prints its child's pid once the child has called setsid, then exits; the child ignores SIGPIPE and ticks every 100 ms.
    private const string EscapedTickerScript = """
        use POSIX;
        $| = 1;
        pipe(my $ready, my $signal) or die;
        my $pid = fork() // die;
        if ($pid == 0) {
            close $ready;
            setsid();
            close $signal;
            $SIG{PIPE} = 'IGNORE';
            while (1) { print "tick\n"; select(undef, undef, undef, 0.1); }
        }
        close $signal;
        <$ready>;
        print "$pid\n";
        """;

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

    [Fact(DisplayName = "StopAsync_EndsAChildThatIgnoresSigint_AfterTheLeaderExited")]
    public async Task StopAsync_EndsAChildThatIgnoresSigint_AfterTheLeaderExited()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var command = new ShellCommand { Command = "/bin/sh" }
            .WithArgumentList("-c", "trap '' INT; (trap '' INT; sleep 60) & echo $!; exit 0")
            .WithOwnProcessGroup();
        await using var process = await _executor.StartProcessAsync(command, TestContext.Current.CancellationToken);
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            var childPid = await backgroundPid;
            (await WhenTrueAsync(() => process.HasExited)).ShouldBeTrue();

            await process.StopAsync(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);

            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StopAsync_GroupThatEndsOnSigint_ReturnsBeforeTheGrace")]
    public async Task StopAsync_GroupThatEndsOnSigint_ReturnsBeforeTheGrace()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(
            SleepCommand.WithOwnProcessGroup(), TestContext.Current.CancellationToken);
        try
        {
            (await CommandNameOnceSettledAsync(process.ProcessId, "sleep")).ShouldEndWith("sleep");

            var elapsed = Stopwatch.StartNew();
            await process.StopAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            elapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
            ProcessGroups.IsEmpty(process.ProcessId).ShouldBeTrue();
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StopAsync_ChildThatTrapsSigterm_GetsSigtermThenSigkill")]
    public async Task StopAsync_ChildThatTrapsSigterm_GetsSigtermThenSigkill()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var command = new ShellCommand { Command = "/bin/sh" }
            .WithArgumentList("-c", "(trap 'echo got TERM' TERM; while :; do sleep 0.1; done) & echo $!; wait")
            .WithOwnProcessGroup();
        await using var process = await _executor.StartProcessAsync(command, TestContext.Current.CancellationToken);
        var lines = new ConcurrentQueue<string>();
        process.OutputReceived += lines.Enqueue;
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            var childPid = await backgroundPid;

            await process.StopAsync(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);

            (await WhenTrueAsync(() => lines.Contains("got TERM"))).ShouldBeTrue();
            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StopAsync_LeaderAliveAtSigkill_KillsAChildThatCalledSetsid")]
    public async Task StopAsync_LeaderAliveAtSigkill_KillsAChildThatCalledSetsid()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");
        Assert.SkipUnless(File.Exists(PerlPath), $"{PerlPath} is missing");

        var command = new ShellCommand { Command = "/bin/sh" }
            .WithArgumentList("-c", $"trap '' INT TERM; {PerlPath} -e 'use POSIX; setsid(); sleep 60' & echo $!; wait")
            .WithOwnProcessGroup();
        await using var process = await _executor.StartProcessAsync(command, TestContext.Current.CancellationToken);
        var printed = new PrintedPids();
        process.OutputReceived += printed.Collect;
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            var escapedPid = await backgroundPid;
            (await WhenTrueAsync(() => ProcessGroups.GroupOf(escapedPid) == escapedPid)).ShouldBeTrue();

            await process.StopAsync(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);

            (await WhenTrueAsync(() => IsGone(escapedPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            process.Kill();
            printed.KillStillRunning();
        }
    }

    [Fact(DisplayName = "StopAsync_EmptyGroup_ReturnsAtOnce")]
    public async Task StopAsync_EmptyGroup_ReturnsAtOnce()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var command = new ShellCommand { Command = "/usr/bin/true" }.WithOwnProcessGroup();
        await using var process = await _executor.StartProcessAsync(command, TestContext.Current.CancellationToken);
        try
        {
            (await WhenTrueAsync(() => process.HasExited)).ShouldBeTrue();

            var elapsed = Stopwatch.StartNew();
            await process.StopAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            elapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StopAsync_Twice_SecondReturnsAtOnce")]
    public async Task StopAsync_Twice_SecondReturnsAtOnce()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(
            BackgroundChildCommand.WithOwnProcessGroup(), TestContext.Current.CancellationToken);
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            await backgroundPid;
            await process.StopAsync(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);

            var second = Stopwatch.StartNew();
            await process.StopAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            second.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));

            var concurrent = Stopwatch.StartNew();
            await Task.WhenAll(
                process.StopAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
                process.StopAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            concurrent.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StopAsync_ConcurrentlyOnALiveGroup_BothReturnAndTheGroupEnds")]
    public async Task StopAsync_ConcurrentlyOnALiveGroup_BothReturnAndTheGroupEnds()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(
            BackgroundChildCommand.WithOwnProcessGroup(), TestContext.Current.CancellationToken);
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            var childPid = await backgroundPid;

            await Task.WhenAll(
                process.StopAsync(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken),
                process.StopAsync(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken));

            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StopAsync_ChildThatCalledSetsid_DoesNotHang")]
    public async Task StopAsync_ChildThatCalledSetsid_DoesNotHang()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");
        Assert.SkipUnless(File.Exists(PerlPath), $"{PerlPath} is missing");

        var command = new ShellCommand { Command = "/bin/sh" }
            .WithArgumentList("-c", $"{PerlPath} -e 'use POSIX; setsid(); sleep 60' & echo $!; wait")
            .WithOwnProcessGroup();
        await using var process = await _executor.StartProcessAsync(command, TestContext.Current.CancellationToken);
        var printed = new PrintedPids();
        process.OutputReceived += printed.Collect;
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            var escapedPid = await backgroundPid;
            (await WhenTrueAsync(() => ProcessGroups.GroupOf(escapedPid) == escapedPid)).ShouldBeTrue();

            var elapsed = Stopwatch.StartNew();
            await process.StopAsync(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);

            elapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3));
        }
        finally
        {
            process.Kill();
            printed.KillStillRunning();
        }
    }

    [Fact(DisplayName = "Kill_GroupLeader_KillsTheGroup")]
    public async Task Kill_GroupLeader_KillsTheGroup()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(OrphanedChildCommand, TestContext.Current.CancellationToken);
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            var childPid = await backgroundPid;

            process.Kill();

            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StopAsyncAndKill_LeaderPidHeldByAnotherStartTime_LeaveTheGroupAlone")]
    public async Task StopAsyncAndKill_LeaderPidHeldByAnotherStartTime_LeaveTheGroupAlone()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = CreateGroupLeader(LeaderExitsLeavingAChildCommand, _ => AnotherStartTime);
        var printed = new PrintedPids();
        process.OutputReceived += printed.Collect;
        var backgroundPid = FirstPidPrinted(process);
        process.Start();
        try
        {
            var childPid = await backgroundPid;
            Assert.SkipWhen(process.StartedAt == DateTimeOffset.MinValue, "The OS gave no start time for the leader to compare");
            (await WhenTrueAsync(() => process.HasExited)).ShouldBeTrue();

            await process.StopAsync(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
            (await WhenTrueAsync(() => IsGone(childPid), SurvivesTimeout)).ShouldBeFalse();

            process.Kill();
            (await WhenTrueAsync(() => IsGone(childPid), SurvivesTimeout)).ShouldBeFalse();
        }
        finally
        {
            printed.KillStillRunning();
        }
    }

    [Fact(DisplayName = "StopAsync_LeaderPidHeldBySameStartTime_StopsTheGroup")]
    public async Task StopAsync_LeaderPidHeldBySameStartTime_StopsTheGroup()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = CreateGroupLeader(LeaderExitsLeavingAChildCommand, leader => leader.StartedAt);
        var printed = new PrintedPids();
        process.OutputReceived += printed.Collect;
        var backgroundPid = FirstPidPrinted(process);
        process.Start();
        try
        {
            var childPid = await backgroundPid;
            Assert.SkipWhen(process.StartedAt == DateTimeOffset.MinValue, "The OS gave no start time for the leader to compare");
            (await WhenTrueAsync(() => process.HasExited)).ShouldBeTrue();

            await process.StopAsync(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);

            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            printed.KillStillRunning();
        }
    }

    [Fact(DisplayName = "Kill_LeaderPidHeldBySameStartTime_KillsTheGroup")]
    public async Task Kill_LeaderPidHeldBySameStartTime_KillsTheGroup()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = CreateGroupLeader(LeaderExitsLeavingAChildCommand, leader => leader.StartedAt);
        var printed = new PrintedPids();
        process.OutputReceived += printed.Collect;
        var backgroundPid = FirstPidPrinted(process);
        process.Start();
        try
        {
            var childPid = await backgroundPid;
            Assert.SkipWhen(process.StartedAt == DateTimeOffset.MinValue, "The OS gave no start time for the leader to compare");
            (await WhenTrueAsync(() => process.HasExited)).ShouldBeTrue();

            process.Kill();

            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            printed.KillStillRunning();
        }
    }

    [Fact(DisplayName = "DisposeAsync_GroupLeader_StopsTheGroup")]
    public async Task DisposeAsync_GroupLeader_StopsTheGroup()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        await using var process = await _executor.StartProcessAsync(OrphanedChildCommand, TestContext.Current.CancellationToken);
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            var childPid = await backgroundPid;

            await process.DisposeAsync();

            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "DisposeAsync_GroupProcessNeverStarted_DoesNotThrow")]
    public async Task DisposeAsync_GroupProcessNeverStarted_DoesNotThrow()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var process = new ShellProcess(new ProcessStartInfo("/bin/sleep", "30"), logger: null, ownProcessGroup: true);

        process.Kill();
        await process.DisposeAsync();
    }

    [Fact(DisplayName = "StartProcess_OwnProcessGroup_ExitedFiresWhileAChildHoldsStdout")]
    public async Task StartProcess_OwnProcessGroup_ExitedFiresWhileAChildHoldsStdout()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var command = new ShellCommand { Command = "/bin/sh" }
            .WithArgumentList("-c", "sleep 60 & echo $!; exit 3")
            .WithOwnProcessGroup();
        await using var process = await _executor.StartProcessAsync(command, TestContext.Current.CancellationToken);
        var exited = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += exitCode => exited.TrySetResult(exitCode);
        var backgroundPid = FirstPidPrinted(process);
        try
        {
            await backgroundPid;

            var exitCode = await exited.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

            exitCode.ShouldBe(3);
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "ExecuteStreaming_OwnProcessGroup_BackgroundChildHoldingStdout_ReturnsWhenTheLeaderExits")]
    public async Task ExecuteStreaming_OwnProcessGroup_BackgroundChildHoldingStdout_ReturnsWhenTheLeaderExits()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var command = new ShellCommand { Command = "/bin/sh" }
            .WithArgumentList("-c", "sleep 60 & echo $!; echo started")
            .WithOwnProcessGroup()
            .WithTimeout(TimeSpan.FromSeconds(30));
        var lines = new ConcurrentQueue<string>();
        var printed = new PrintedPids();
        try
        {
            var result = await _executor.ExecuteStreamingAsync(
                command,
                line =>
                {
                    lines.Enqueue(line);
                    printed.Collect(line);
                },
                cancellationToken: TestContext.Current.CancellationToken);

            var childPid = printed.All.ShouldHaveSingleItem();
            printed.SinceFirstPrinted.ShouldBeLessThan(TimeSpan.FromSeconds(5));
            result.ExitCode.ShouldBe(0);
            lines.ShouldContain("started");
            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            printed.KillStillRunning();
        }
    }

    // A slow reader leaves the leader's lines in the pipe after it exits, so they arrive only while the call drains.
    [Fact(DisplayName = "ExecuteStreaming_OwnProcessGroup_SlowReader_GetsEveryLine")]
    public async Task ExecuteStreaming_OwnProcessGroup_SlowReader_GetsEveryLine()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var command = new ShellCommand { Command = "/usr/bin/seq" }
            .WithArgumentList("1", "100")
            .WithOwnProcessGroup()
            .WithTimeout(TimeSpan.FromSeconds(30));
        var lines = new ConcurrentQueue<string>();

        var result = await _executor.ExecuteStreamingAsync(
            command,
            line =>
            {
                Thread.Sleep(2);
                lines.Enqueue(line);
            },
            cancellationToken: TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        lines.Count.ShouldBe(100);
    }

    // Without the login shell wrapper, whose setup takes about as long as the timeout on macOS.
    [Fact(DisplayName = "ExecuteStreaming_OwnProcessGroup_Timeout_EndsTheGroup")]
    public async Task ExecuteStreaming_OwnProcessGroup_Timeout_EndsTheGroup()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var command = new ShellCommand { Command = "/bin/sh" }
            .WithArgumentList("-c", "sleep 60 & echo $$; echo $!; sleep 60")
            .WithOwnProcessGroup()
            .WithoutShell()
            .WithTimeout(TimeSpan.FromMilliseconds(500));
        var printed = new PrintedPids();
        try
        {
            await Should.ThrowAsync<OperationCanceledException>(() => _executor.ExecuteStreamingAsync(
                command, printed.Collect, cancellationToken: TestContext.Current.CancellationToken));

            printed.All.Count.ShouldBe(2);
            var (leaderPid, childPid) = (printed.All[0], printed.All[1]);
            (await WhenTrueAsync(() => ProcessGroups.IsEmpty(leaderPid), GoneTimeout)).ShouldBeTrue();
            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            printed.KillStillRunning();
        }
    }

    // A SIGKILL to the tree would end the child before its trap could print.
    [Fact(DisplayName = "ExecuteStreaming_OwnProcessGroup_Timeout_StopsTheGroupGracefully")]
    public async Task ExecuteStreaming_OwnProcessGroup_Timeout_StopsTheGroupGracefully()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var command = new ShellCommand { Command = "/bin/sh" }
            .WithArgumentList("-c", "(trap 'echo got TERM; exit 0' TERM; while :; do sleep 0.1; done) & echo $!; wait")
            .WithOwnProcessGroup()
            .WithoutShell()
            .WithTimeout(TimeSpan.FromMilliseconds(500));
        var lines = new ConcurrentQueue<string>();
        var printed = new PrintedPids();
        try
        {
            await Should.ThrowAsync<OperationCanceledException>(() => _executor.ExecuteStreamingAsync(
                command,
                line =>
                {
                    lines.Enqueue(line);
                    printed.Collect(line);
                },
                cancellationToken: TestContext.Current.CancellationToken));

            lines.ShouldContain("got TERM");
            var childPid = printed.All.ShouldHaveSingleItem();
            (await WhenTrueAsync(() => IsGone(childPid), GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            printed.KillStillRunning();
        }
    }

    [Fact(DisplayName = "ExecuteStreaming_OwnProcessGroup_ChildThatLeftTheGroup_NoCallbackAfterTheCallReturns")]
    public async Task ExecuteStreaming_OwnProcessGroup_ChildThatLeftTheGroup_NoCallbackAfterTheCallReturns()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");
        Assert.SkipUnless(File.Exists(PerlPath), $"{PerlPath} is missing");

        var command = new ShellCommand { Command = PerlPath }
            .WithArgumentList("-e", EscapedTickerScript)
            .WithOwnProcessGroup()
            .WithoutShell()
            .WithTimeout(TimeSpan.FromSeconds(30));
        var callbacks = 0;
        var printed = new PrintedPids();
        try
        {
            await _executor.ExecuteStreamingAsync(
                command,
                line =>
                {
                    Interlocked.Increment(ref callbacks);
                    printed.Collect(line);
                },
                cancellationToken: TestContext.Current.CancellationToken);

            var afterReturn = Volatile.Read(ref callbacks);
            var escapedPid = printed.All.ShouldHaveSingleItem();
            await Task.Delay(SurvivesTimeout, TestContext.Current.CancellationToken);

            IsGone(escapedPid).ShouldBeFalse();
            Volatile.Read(ref callbacks).ShouldBe(afterReturn);
        }
        finally
        {
            printed.KillStillRunning();
        }
    }

    // Not started, so a test can subscribe first. The pid-reuse check sees startTimeOfLeader in place of the OS's answer.
    private ShellProcess CreateGroupLeader(ShellCommand command, Func<ShellProcess, DateTimeOffset?> startTimeOfLeader)
    {
        ShellProcess? process = null;
        process = new ShellProcess(
            _executor.CreateProcessStartInfo(command, redirectInput: true),
            logger: null,
            ownProcessGroup: true,
            startTimeOf: _ => startTimeOfLeader(process!),
            createJob: null);
        return process;
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

    private static bool IsGone(int pid) => !ProcessGroups.SignalProcess(pid, 0);

    private static async Task<bool> WhenTrueAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed > (timeout ?? SettleTimeout))
                return false;
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        return true;
    }

    // Each pid's start time is read as it is printed, so cleanup never signals a later process given the same pid.
    private sealed class PrintedPids
    {
        private static readonly TimeSpan SameStartTolerance = TimeSpan.FromSeconds(1);

        private readonly ConcurrentQueue<(int Pid, DateTimeOffset? StartedAt, long PrintedAt)> _printed = new();

        public IReadOnlyList<int> All => [.. _printed.Select(printed => printed.Pid)];

        public TimeSpan SinceFirstPrinted => Stopwatch.GetElapsedTime(_printed.First().PrintedAt);

        public void Collect(string line)
        {
            var printedAt = Stopwatch.GetTimestamp();
            if (int.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out var pid))
                _printed.Enqueue((pid, ProcessGroups.StartTimeOf(pid), printedAt));
        }

        public void KillStillRunning()
        {
            foreach (var (pid, startedAt, _) in _printed)
            {
                if (startedAt is { } then && then != DateTimeOffset.MinValue
                    && ProcessGroups.StartTimeOf(pid) is { } now && (now - then).Duration() < SameStartTolerance)
                {
                    ProcessGroups.SignalProcess(pid, ProcessGroups.SIGKILL);
                }
            }
        }
    }
}
