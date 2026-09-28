using System.Text;
using Mythetech.Framework.Desktop.Secrets;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Secrets;

/// <summary>
/// Runs the real process runner against stand-in executables, never the 1Password CLI, to check that
/// arguments arrive verbatim and that stdin reaches the child.
/// </summary>
public class OnePasswordCliRunnerTests
{
    private static string EchoArgsPath => Path.Combine(
        AppContext.BaseDirectory,
        OperatingSystem.IsWindows() ? "Mythetech.Framework.Test.EchoArgs.exe" : "Mythetech.Framework.Test.EchoArgs");

    [Fact(DisplayName = "Arguments reach the process verbatim, one value per argument")]
    public async Task RunAsync_PassesArgumentsVerbatim()
    {
        string[] arguments = ["item", "get", "Aion connection 1", "say \"hi\"", "trailing\\", "日本語"];
        var runner = new OnePasswordCliRunner(EchoArgsPath);

        var result = await runner.RunAsync(arguments, null, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0, result.StandardError);
        DecodeArgs(result.StandardOutput).ShouldBe(arguments);
    }

    [Fact(DisplayName = "Standard input is written to the process and closed")]
    public async Task RunAsync_WritesStandardInput()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses /bin/cat to echo stdin");
        const string payload = """{"fields":[{"id":"password","value":"s3cr3t \"☃\" \\ end"}]}""";
        var runner = new OnePasswordCliRunner("/bin/cat");

        var result = await runner.RunAsync([], payload, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.Trim().ShouldBe(payload);
    }

    [Fact(DisplayName = "A failing process reports its exit code and standard error")]
    public async Task RunAsync_ReportsExitCodeAndStandardError()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses /bin/sh");
        var runner = new OnePasswordCliRunner("/bin/sh");

        var result = await runner.RunAsync(["-c", "echo denied >&2; exit 3"], null, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(3);
        result.StandardError.Trim().ShouldBe("denied");
    }

    [Fact(DisplayName = "A process that exits without reading stdin still reports its result")]
    public async Task RunAsync_ProcessIgnoresStandardInput_StillReturns()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses /bin/sh");
        var runner = new OnePasswordCliRunner("/bin/sh");
        var largePayload = new string('x', 1024 * 1024);

        var result = await runner.RunAsync(["-c", "echo locked >&2; exit 1"], largePayload, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(1);
        result.StandardError.Trim().ShouldBe("locked");
    }

    private static List<string> DecodeArgs(string output) =>
        output.Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.StartsWith("arg:", StringComparison.Ordinal))
            .Select(line => Encoding.UTF8.GetString(Convert.FromBase64String(line["arg:".Length..])))
            .ToList();
}
