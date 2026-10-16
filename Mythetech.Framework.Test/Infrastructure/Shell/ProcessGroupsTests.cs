using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

[UnsupportedOSPlatform("windows")]
public class ProcessGroupsTests
{
    private const int LeaderPid = 4242;
    private const int NoSuchPid = int.MaxValue;
    private const int InvalidSignal = 999;
    private const int EINVAL = 22;

    private static readonly DateTimeOffset LeaderStartedAt = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "MayStillOwnGroup_LeaderGone_True")]
    public void MayStillOwnGroup_LeaderGone_True()
    {
        ProcessGroups.MayStillOwnGroup(LeaderPid, LeaderStartedAt, StartTimeOfLeader(null)).ShouldBeTrue();
    }

    [Fact(DisplayName = "MayStillOwnGroup_SameStartTime_True")]
    public void MayStillOwnGroup_SameStartTime_True()
    {
        ProcessGroups.MayStillOwnGroup(LeaderPid, LeaderStartedAt, StartTimeOfLeader(LeaderStartedAt)).ShouldBeTrue();
        ProcessGroups.MayStillOwnGroup(LeaderPid, LeaderStartedAt, StartTimeOfLeader(LeaderStartedAt.AddMilliseconds(10)))
            .ShouldBeTrue();
    }

    [Fact(DisplayName = "MayStillOwnGroup_OtherStartTime_False")]
    public void MayStillOwnGroup_OtherStartTime_False()
    {
        ProcessGroups.MayStillOwnGroup(LeaderPid, LeaderStartedAt, StartTimeOfLeader(LeaderStartedAt.AddSeconds(2)))
            .ShouldBeFalse();
        ProcessGroups.MayStillOwnGroup(LeaderPid, LeaderStartedAt, StartTimeOfLeader(LeaderStartedAt.AddSeconds(-2)))
            .ShouldBeFalse();
    }

    [Fact(DisplayName = "MayStillOwnGroup_UnreadableStartTime_False")]
    public void MayStillOwnGroup_UnreadableStartTime_False()
    {
        ProcessGroups.MayStillOwnGroup(LeaderPid, DateTimeOffset.MinValue, StartTimeOfLeader(DateTimeOffset.MinValue))
            .ShouldBeFalse();
        ProcessGroups.MayStillOwnGroup(LeaderPid, DateTimeOffset.MinValue, StartTimeOfLeader(LeaderStartedAt)).ShouldBeFalse();
        ProcessGroups.MayStillOwnGroup(LeaderPid, LeaderStartedAt, StartTimeOfLeader(DateTimeOffset.MinValue)).ShouldBeFalse();
    }

    // With no process on the pid there is nothing to compare, so the leader's own reading doesn't matter.
    [Fact(DisplayName = "MayStillOwnGroup_LeaderStartTimeUnreadableAndLeaderGone_True")]
    public void MayStillOwnGroup_LeaderStartTimeUnreadableAndLeaderGone_True()
    {
        ProcessGroups.MayStillOwnGroup(LeaderPid, DateTimeOffset.MinValue, StartTimeOfLeader(null)).ShouldBeTrue();
    }

    // Signal 0 only checks, so a missing guard can't reach every process the user owns.
    [Fact(DisplayName = "Signal_GroupIdOneOrLess_Throws")]
    public void Signal_GroupIdOneOrLess_Throws()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        Should.Throw<ArgumentOutOfRangeException>(() => ProcessGroups.Signal(1, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => ProcessGroups.Signal(0, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => ProcessGroups.Signal(-1, 0));
    }

    [Fact(DisplayName = "Signal_NoSuchGroup_False")]
    public void Signal_NoSuchGroup_False()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        ProcessGroups.Signal(NoSuchPid, 0).ShouldBeFalse();
    }

    // The kernel refuses an out-of-range signal with EINVAL before delivering anything, so the sleep is not signalled.
    [Fact(DisplayName = "Signal_FailsForAnotherReasonThanNoSuchGroup_LogsTheErrno")]
    public async Task Signal_FailsForAnotherReasonThanNoSuchGroup_LogsTheErrno()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        var command = new ShellCommand { Command = "/bin/sleep" }.WithArgumentList("30").WithOwnProcessGroup().WithoutShell();
        await using var process = await new ShellExecutor().StartProcessAsync(command, TestContext.Current.CancellationToken);
        var logger = new RecordingLogger();
        try
        {
            ProcessGroups.Signal(process.ProcessId, InvalidSignal, logger).ShouldBeFalse();

            var entry = logger.Entries.ShouldHaveSingleItem();
            entry.Level.ShouldBe(LogLevel.Debug);
            entry.Message.ShouldContain($"errno {EINVAL}");
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "Signal_NoSuchGroup_LogsNothing")]
    public void Signal_NoSuchGroup_LogsNothing()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");
        var logger = new RecordingLogger();

        ProcessGroups.Signal(NoSuchPid, 0, logger).ShouldBeFalse();

        logger.Entries.ShouldBeEmpty();
    }

    [Fact(DisplayName = "SignalProcess_PidZeroOrLess_Throws")]
    public void SignalProcess_PidZeroOrLess_Throws()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX signals");

        Should.Throw<ArgumentOutOfRangeException>(() => ProcessGroups.SignalProcess(0, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => ProcessGroups.SignalProcess(-1, 0));
    }

    [Fact(DisplayName = "StartTimeOf_ThisProcess_MatchesTheOs")]
    public void StartTimeOf_ThisProcess_MatchesTheOs()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");
        using var current = Process.GetCurrentProcess();
        DateTimeOffset osStartedAt = current.StartTime.ToUniversalTime();

        var startedAt = ProcessGroups.StartTimeOf(Environment.ProcessId);

        startedAt.ShouldNotBeNull();
        (startedAt.Value - osStartedAt).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "StartTimeOf_NoSuchProcess_Null")]
    public void StartTimeOf_NoSuchProcess_Null()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "POSIX process groups");

        ProcessGroups.StartTimeOf(NoSuchPid).ShouldBeNull();
    }

    private static Func<int, DateTimeOffset?> StartTimeOfLeader(DateTimeOffset? startedAt) => pid =>
    {
        pid.ShouldBe(LeaderPid);
        return startedAt;
    };

    private sealed class RecordingLogger : ILogger
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Enqueue((logLevel, formatter(state, exception)));
    }
}
