using System.Text;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

/// <summary>
/// Runs real processes through <see cref="ShellExecutor"/> and checks the argv the child receives.
/// </summary>
public class ShellExecutorArgumentTests
{
    private static readonly string[] TrickyValues =
    [
        "plain",
        "",
        "with space",
        "it's",
        "say \"hi\"",
        @"C:\dir\",
        @"C:\Users\tom",
        "trailing\\",
        "a\\\"b",
        "100%",
        "%PATH%",
        "a^b",
        "hi!",
        "$HOME",
        "`whoami`",
        "$(whoami)",
        "a;b|c&d",
        "*.txt",
        "~",
        "line1\nline2",
        "日本語 🚀",
        "{\"mcpServers\":{\"fs\":{\"command\":\"C:\\\\tools\\\\fs.exe\"}}}",
        "--flag=value",
    ];

    private readonly ShellExecutor _executor = new();

    private static string EchoArgsPath => Path.Combine(
        AppContext.BaseDirectory,
        OperatingSystem.IsWindows() ? "Mythetech.Framework.Test.EchoArgs.exe" : "Mythetech.Framework.Test.EchoArgs");

    [Fact(DisplayName = "ArgumentList_DirectExecution_ChildReceivesValuesVerbatim")]
    public async Task ArgumentList_DirectExecution_ChildReceivesValuesVerbatim()
    {
        var command = new ShellCommand { Command = EchoArgsPath }
            .WithArgumentList(TrickyValues)
            .WithoutShell();

        var result = await _executor.ExecuteAsync(command, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0, result.StandardError);
        DecodeArgs(result.StandardOutput).ShouldBe(TrickyValues);
    }

    [Fact(DisplayName = "ArgumentList_ShellExecution_ChildReceivesValuesVerbatim")]
    public async Task ArgumentList_ShellExecution_ChildReceivesValuesVerbatim()
    {
        var command = new ShellCommand { Command = EchoArgsPath }.WithArgumentList(TrickyValues);

        var result = await _executor.ExecuteAsync(command, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0, result.StandardError);
        DecodeArgs(result.StandardOutput).ShouldBe(TrickyValues);
    }

    [Fact(DisplayName = "ArgumentList_ShellExecution_CommandPathWithSpacesIsQuoted")]
    public async Task ArgumentList_ShellExecution_CommandPathWithSpacesIsQuoted()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses a POSIX shell script as the command");

        var directory = Directory.CreateTempSubdirectory("shell exec $test");
        try
        {
            var script = Path.Combine(directory.FullName, "echo args");
            await File.WriteAllTextAsync(
                script,
                $"#!/bin/sh\nexec {ShellQuoting.Quote(EchoArgsPath)} \"$@\"\n",
                TestContext.Current.CancellationToken);
            File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            var command = new ShellCommand { Command = script }.WithArgumentList("a b", "$HOME");

            var result = await _executor.ExecuteAsync(command, TestContext.Current.CancellationToken);

            result.ExitCode.ShouldBe(0, result.StandardError);
            DecodeArgs(result.StandardOutput).ShouldBe(["a b", "$HOME"]);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact(DisplayName = "QuoteWindows_ArgumentsString_ChildReceivesValuesVerbatimOnWindows")]
    public async Task QuoteWindows_ArgumentsString_ChildReceivesValuesVerbatimOnWindows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows command-line parsing only applies on Windows");

        var command = new ShellCommand
        {
            Command = EchoArgsPath,
            Arguments = string.Join(" ", TrickyValues.Select(ShellQuoting.QuoteWindows)),
        };

        var result = await _executor.ExecuteAsync(command, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0, result.StandardError);
        DecodeArgs(result.StandardOutput).ShouldBe(TrickyValues);
    }

    [Theory(DisplayName = "ValidateBatchFileArguments_UnsafeArgumentForBatchFile_Throws")]
    [InlineData("npm.cmd", "100% sure")]
    [InlineData(@"C:\tools\claude.CMD", "a&calc")]
    [InlineData("build.bat", "\"quoted\"")]
    [InlineData("build.bat", "hi!")]
    [InlineData("build.bat", "a^b")]
    [InlineData("build.bat", "a|b")]
    [InlineData("build.bat", "line1\nline2")]
    [InlineData("build.bat", "trailing\n")]
    public void ValidateBatchFileArguments_UnsafeArgumentForBatchFile_Throws(string command, string argument)
    {
        Should.Throw<NotSupportedException>(() => ShellExecutor.ValidateBatchFileArguments(command, ["ok", argument]));
    }

    [Theory(DisplayName = "ValidateBatchFileArguments_SafeArgumentsForBatchFile_DoesNotThrow")]
    [InlineData("npm.cmd", "install")]
    [InlineData("npm.cmd", "left-pad@1.3.0")]
    [InlineData("npm.cmd", "--save-dev")]
    [InlineData(@"C:\tools\claude.cmd", @"C:\Users\tom\My Project")]
    [InlineData("build.bat", "--model=opus,fast")]
    [InlineData("build.bat", "")]
    public void ValidateBatchFileArguments_SafeArgumentsForBatchFile_DoesNotThrow(string command, string argument)
    {
        Should.NotThrow(() => ShellExecutor.ValidateBatchFileArguments(command, [argument]));
    }

    [Theory(DisplayName = "ValidateBatchFileArguments_NonBatchCommand_AllowsAnyArgument")]
    [InlineData("claude.exe")]
    [InlineData("claude")]
    [InlineData(@"C:\tools\git.EXE")]
    public void ValidateBatchFileArguments_NonBatchCommand_AllowsAnyArgument(string command)
    {
        Should.NotThrow(() => ShellExecutor.ValidateBatchFileArguments(command, ["100% & \"quoted\" ^!"]));
    }

    private static List<string> DecodeArgs(string output) =>
        output.Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.StartsWith("arg:", StringComparison.Ordinal))
            .Select(line => Encoding.UTF8.GetString(Convert.FromBase64String(line["arg:".Length..])))
            .ToList();
}
