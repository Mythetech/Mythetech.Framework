using System.Diagnostics;
using System.Runtime.Versioning;
using Mythetech.Framework.Desktop.Services;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

[UnsupportedOSPlatform("windows")]
public class ProcessGroupsTests
{
    private const int LeaderPid = 4242;
    private const int NoSuchPid = int.MaxValue;

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
}
