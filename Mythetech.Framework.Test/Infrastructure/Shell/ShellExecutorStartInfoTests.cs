using System.Runtime.Versioning;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

/// <summary>
/// Pins the platform-specific settings <see cref="ShellExecutor"/> starts a group command with.
/// </summary>
public class ShellExecutorStartInfoTests
{
    private static readonly ShellCommand Command = new ShellCommand { Command = "tool" }.WithArgumentList("--flag");

    private readonly ShellExecutor _executor = new();

    [Fact(DisplayName = "CreateProcessStartInfo_OwnProcessGroupOnUnix_StartsDetached")]
    [UnsupportedOSPlatform("windows")]
    public void CreateProcessStartInfo_OwnProcessGroupOnUnix_StartsDetached()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix start settings");

        _executor.CreateProcessStartInfo(Command.WithOwnProcessGroup()).StartDetached.ShouldBeTrue();
        _executor.CreateProcessStartInfo(Command).StartDetached.ShouldBeFalse();

        if (OperatingSystem.IsMacOS())
        {
            _executor.CreateProcessStartInfo(Command.WithOwnProcessGroup()).StartSuspended.ShouldBeFalse();
        }
    }

    [Fact(DisplayName = "CreateProcessStartInfo_OwnProcessGroupOnLinux_KillsOnParentExit")]
    [SupportedOSPlatform("linux")]
    public void CreateProcessStartInfo_OwnProcessGroupOnLinux_KillsOnParentExit()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux start settings");

        _executor.CreateProcessStartInfo(Command.WithOwnProcessGroup()).KillOnParentExit.ShouldBeTrue();
        _executor.CreateProcessStartInfo(Command).KillOnParentExit.ShouldBeFalse();
    }

    [Fact(DisplayName = "CreateProcessStartInfo_OwnProcessGroupOnWindows_StartsSuspendedNeverDetached")]
    [SupportedOSPlatform("windows")]
    public void CreateProcessStartInfo_OwnProcessGroupOnWindows_StartsSuspendedNeverDetached()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows start settings");

        var group = _executor.CreateProcessStartInfo(Command.WithOwnProcessGroup());
        group.StartSuspended.ShouldBeTrue();
        group.StartDetached.ShouldBeFalse();

        var plain = _executor.CreateProcessStartInfo(Command);
        plain.StartSuspended.ShouldBeFalse();
        plain.StartDetached.ShouldBeFalse();
    }
}
