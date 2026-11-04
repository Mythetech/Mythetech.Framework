using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

public class ShellProcessOutputTests
{
    [Fact(DisplayName = "StartProcessAsync_SubscribedAfterTheProcessExited_StillGetsItsOutput")]
    public async Task StartProcessAsync_SubscribedAfterTheProcessExited_StillGetsItsOutput()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses /bin/sh");
        var ct = TestContext.Current.CancellationToken;
        var command = new ShellCommand { Command = "/bin/sh" }
            .WithArgumentList("-c", "echo out1; echo err1 >&2; echo out2")
            .WithoutShell();

        await using var process = await new ShellExecutor().StartProcessAsync(command, ct);
        await process.WaitForExitAsync(ct);
        var output = new List<string>();
        var error = new List<string>();
        process.OutputReceived += output.Add;
        process.ErrorReceived += error.Add;

        output.ShouldBe(["out1", "out2"]);
        error.ShouldBe(["err1"]);
    }
}
