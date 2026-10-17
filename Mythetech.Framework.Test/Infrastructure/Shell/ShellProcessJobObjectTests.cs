using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

/// <summary>
/// Starts real processes through <see cref="ShellExecutor"/> on Windows and checks the job object a group command runs in.
/// </summary>
public class ShellProcessJobObjectTests
{
    private static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan GoneTimeout = TimeSpan.FromSeconds(5);

    private readonly ShellExecutor _executor = new();

    // cmd exits as soon as start has launched ping, so only the job still reaches ping.
    private static ShellCommand OrphanedPingCommand =>
        new ShellCommand { Command = "cmd.exe", Arguments = "/c start /b ping -n 60 127.0.0.1 > nul" }.WithOwnProcessGroup();

    [Fact(DisplayName = "StopAsync_GroupLeader_EndsAGrandchildAfterTheLeaderExited")]
    [SupportedOSPlatform("windows")]
    public async Task StopAsync_GroupLeader_EndsAGrandchildAfterTheLeaderExited()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Job objects");

        await using var process = await _executor.StartProcessAsync(OrphanedPingCommand, TestContext.Current.CancellationToken);
        using var ping = await ChildNamedAsync(process.ProcessId, "PING.EXE");
        try
        {
            process.OwnsProcessGroup.ShouldBeTrue();
            (await WhenTrueAsync(() => process.HasExited)).ShouldBeTrue();
            ping.HasExited.ShouldBeFalse();

            var elapsed = Stopwatch.StartNew();
            await process.StopAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            elapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
            (await ExitsWithinAsync(ping, GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            KillQuietly(ping);
        }
    }

    [Fact(DisplayName = "Kill_GroupLeader_TerminatesTheJob")]
    [SupportedOSPlatform("windows")]
    public async Task Kill_GroupLeader_TerminatesTheJob()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Job objects");

        await using var process = await _executor.StartProcessAsync(OrphanedPingCommand, TestContext.Current.CancellationToken);
        using var ping = await ChildNamedAsync(process.ProcessId, "PING.EXE");
        try
        {
            (await WhenTrueAsync(() => process.HasExited)).ShouldBeTrue();

            process.Kill();

            (await ExitsWithinAsync(ping, GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            KillQuietly(ping);
        }
    }

    [Fact(DisplayName = "DisposeAsync_GroupLeader_EndsAGrandchild")]
    [SupportedOSPlatform("windows")]
    public async Task DisposeAsync_GroupLeader_EndsAGrandchild()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Job objects");

        var process = await _executor.StartProcessAsync(OrphanedPingCommand, TestContext.Current.CancellationToken);
        using var ping = await ChildNamedAsync(process.ProcessId, "PING.EXE");
        try
        {
            await process.DisposeAsync();

            (await ExitsWithinAsync(ping, GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            KillQuietly(ping);
        }
    }

    // The process starts suspended, so its output shows it was resumed with its redirected streams intact.
    [Fact(DisplayName = "StartProcess_OwnProcessGroupOnWindows_RunsAndStreamsOutput")]
    [SupportedOSPlatform("windows")]
    public async Task StartProcess_OwnProcessGroupOnWindows_RunsAndStreamsOutput()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Job objects");

        var command = new ShellCommand { Command = "cmd.exe", Arguments = "/c ping -n 2 127.0.0.1 > nul & echo started" }
            .WithOwnProcessGroup();
        await using var process = await _executor.StartProcessAsync(command, TestContext.Current.CancellationToken);
        var lines = new ConcurrentQueue<string>();
        process.OutputReceived += lines.Enqueue;

        var exitCode = await process.WaitForExitAsync(TestContext.Current.CancellationToken)
            .WaitAsync(SettleTimeout, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(0);
        lines.ShouldContain(line => line.Trim() == "started");
        process.OwnsProcessGroup.ShouldBeTrue();
    }

    [Fact(DisplayName = "ExecuteAsync_OwnProcessGroupOnWindows_ResumesTheCommand")]
    [SupportedOSPlatform("windows")]
    public async Task ExecuteAsync_OwnProcessGroupOnWindows_ResumesTheCommand()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Job objects");

        var command = new ShellCommand { Command = "cmd.exe", Arguments = "/c echo started" }
            .WithOwnProcessGroup()
            .WithTimeout(SettleTimeout);

        var result = await _executor.ExecuteAsync(command, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.Trim().ShouldBe("started");
    }

    // cmd waits for ping, so the root is still alive to lead the tree kill to ping.
    [Fact(DisplayName = "StopAsync_GroupCommandWithoutAJob_KillsTheTreeAfterTheGrace")]
    [SupportedOSPlatform("windows")]
    public async Task StopAsync_GroupCommandWithoutAJob_KillsTheTreeAfterTheGrace()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Job objects");

        var command = new ShellCommand { Command = "cmd.exe", Arguments = "/c ping -n 60 127.0.0.1 > nul" };
        await using var process = CreateGroupCommandWhoseJobFails(command, new Win32Exception(5));
        process.Start();
        using var ping = await ChildNamedAsync(process.ProcessId, "PING.EXE");
        try
        {
            process.OwnsProcessGroup.ShouldBeFalse();

            await process.StopAsync(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);

            (await ExitsWithinAsync(ping, GoneTimeout)).ShouldBeTrue();
        }
        finally
        {
            KillQuietly(ping);
        }
    }

    // The command starts suspended, so its output shows it was resumed even though no job could be made.
    [Fact(DisplayName = "StartProcess_JobCannotBeMadeForAnyReason_ResumesTheCommandWithoutOne")]
    [SupportedOSPlatform("windows")]
    public async Task StartProcess_JobCannotBeMadeForAnyReason_ResumesTheCommandWithoutOne()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Job objects");

        var command = new ShellCommand { Command = "cmd.exe", Arguments = "/c echo started" };
        await using var process = CreateGroupCommandWhoseJobFails(command, new InvalidOperationException("No job"));
        var lines = new ConcurrentQueue<string>();
        process.OutputReceived += lines.Enqueue;
        process.Start();

        var exitCode = await process.WaitForExitAsync(TestContext.Current.CancellationToken)
            .WaitAsync(SettleTimeout, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(0);
        lines.ShouldContain(line => line.Trim() == "started");
        process.OwnsProcessGroup.ShouldBeFalse();
    }

    // The command starts suspended, so its output shows it was resumed although the warning could not be logged.
    [Fact(DisplayName = "StartProcess_JobFailsAndTheLoggerThrows_ResumesTheCommand")]
    [SupportedOSPlatform("windows")]
    public async Task StartProcess_JobFailsAndTheLoggerThrows_ResumesTheCommand()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Job objects");

        var command = new ShellCommand { Command = "cmd.exe", Arguments = "/c echo started" };
        await using var process = CreateGroupCommandWhoseJobFails(
            command, new InvalidOperationException("No job"), new ThrowingLogger());
        var lines = new ConcurrentQueue<string>();
        process.OutputReceived += lines.Enqueue;
        process.Start();

        var exitCode = await process.WaitForExitAsync(TestContext.Current.CancellationToken)
            .WaitAsync(SettleTimeout, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(0);
        lines.ShouldContain(line => line.Trim() == "started");
        process.OwnsProcessGroup.ShouldBeFalse();
    }

    // Not suspended, so it runs off Windows too: a logger that throws must not escape the no-job path.
    [Fact(DisplayName = "JoinNewJob_JobFailsAndTheLoggerThrows_ReturnsWithoutAJob")]
    public void JoinNewJob_JobFailsAndTheLoggerThrows_ReturnsWithoutAJob()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "StartProcess_JobFailsAndTheLoggerThrows_ResumesTheCommand covers Windows");

        using var process = Process.Start(new ProcessStartInfo("/bin/sleep", "30"))!;
        try
        {
#pragma warning disable CA1416
            var job = WindowsJobObject.JoinNewJob(
                process, new ThrowingLogger(), () => throw new InvalidOperationException("No job"));
#pragma warning restore CA1416

            job.ShouldBeNull();
        }
        finally
        {
            process.Kill();
        }
    }

    [Fact(DisplayName = "StartProcess_WithoutOwnProcessGroupOnWindows_HasNoJob")]
    [SupportedOSPlatform("windows")]
    public async Task StartProcess_WithoutOwnProcessGroupOnWindows_HasNoJob()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Job objects");

        var command = new ShellCommand { Command = "cmd.exe", Arguments = "/c exit 0" };
        await using var process = await _executor.StartProcessAsync(command, TestContext.Current.CancellationToken);

        process.OwnsProcessGroup.ShouldBeFalse();
    }

    // winnt.h: 144 bytes with 8-byte pointers (x64, arm64), 112 with 4-byte ones (x86). Runs on every OS.
    [Fact(DisplayName = "ExtendedLimitInformation_Layout_MatchesWin32")]
    public void ExtendedLimitInformation_Layout_MatchesWin32()
    {
#pragma warning disable CA1416
        var is64Bit = IntPtr.Size == 8;

        Marshal.SizeOf<WindowsJobObject.JOBOBJECT_BASIC_LIMIT_INFORMATION>().ShouldBe(is64Bit ? 64 : 48);
        Marshal.OffsetOf<WindowsJobObject.JOBOBJECT_BASIC_LIMIT_INFORMATION>(
            nameof(WindowsJobObject.JOBOBJECT_BASIC_LIMIT_INFORMATION.LimitFlags)).ShouldBe(16);
        Marshal.SizeOf<WindowsJobObject.IO_COUNTERS>().ShouldBe(48);
        Marshal.OffsetOf<WindowsJobObject.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(
            nameof(WindowsJobObject.JOBOBJECT_EXTENDED_LIMIT_INFORMATION.IoInfo)).ShouldBe(is64Bit ? 64 : 48);
        Marshal.SizeOf<WindowsJobObject.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>().ShouldBe(is64Bit ? 144 : 112);
#pragma warning restore CA1416
    }

    // Not started, so a test can subscribe first. Making its job throws, as on a machine that refuses one.
    [SupportedOSPlatform("windows")]
    private ShellProcess CreateGroupCommandWhoseJobFails(ShellCommand command, Exception jobFailure, ILogger? logger = null) =>
        new(
            _executor.CreateProcessStartInfo(command.WithOwnProcessGroup(), redirectInput: true),
            logger,
            ownProcessGroup: true,
            startTimeOf: null,
            createJob: () => throw jobFailure);

    // Windows keeps a child's parent pid after the parent exits, and the leader's open handle stops its pid being reused.
    [SupportedOSPlatform("windows")]
    private async Task<Process> ChildNamedAsync(int parentPid, string name)
    {
        var query = new ShellCommand { Command = "powershell.exe" }.WithArgumentList(
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            $"Get-CimInstance Win32_Process -Filter 'ParentProcessId={parentPid}' | Where-Object Name -eq '{name}' | ForEach-Object ProcessId")
            .WithTimeout(SettleTimeout);
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < SettleTimeout)
        {
            var result = await _executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
            foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out var pid))
                    return Process.GetProcessById(pid);
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException($"No {name} child of process {parentPid} appeared within {SettleTimeout}");
    }

    private static async Task<bool> ExitsWithinAsync(Process process, TimeSpan timeout)
    {
        try
        {
            await process.WaitForExitAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static void KillQuietly(Process process)
    {
        try
        {
            process.Kill();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // Already gone.
        }
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

    private sealed class ThrowingLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            throw new InvalidOperationException("The logger failed");
    }
}
