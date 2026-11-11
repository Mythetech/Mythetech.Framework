namespace Mythetech.Framework.Test.Infrastructure.Shell;

internal static class ProcessGroupSupport
{
    /// <summary>
    /// Skips a test that starts a group command, which the executor refuses on .NET 10.
    /// </summary>
    public static void SkipOnNet10() =>
        Assert.SkipWhen(Environment.Version.Major < 11, "ShellCommand.OwnProcessGroup needs .NET 11");
}
