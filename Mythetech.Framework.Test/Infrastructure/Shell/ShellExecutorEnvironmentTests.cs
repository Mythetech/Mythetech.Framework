using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

/// <summary>
/// Runs tests that change the test process's own environment, so nothing else starts a process meanwhile.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection
{
    public const string Name = "Process environment";
}

/// <summary>
/// Pins that a command never inherits the MSBuild variables the dotnet CLI sets in an app it started.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ShellExecutorEnvironmentTests : IDisposable
{
    private const string UnrelatedVariable = "MYTHETECH_SHELL_TEST_VARIABLE";

    private static readonly string[] SdkVariables = ["MSBuildExtensionsPath", "MSBuildSDKsPath", "MSBUILD_EXE_PATH"];

    private readonly Dictionary<string, string?> _original =
        SdkVariables.Append(UnrelatedVariable).ToDictionary(name => name, Environment.GetEnvironmentVariable);

    private readonly ShellExecutor _executor = new();

    public ShellExecutorEnvironmentTests()
    {
        // What `dotnet run` and `dotnet test` hand the app they start.
        Environment.SetEnvironmentVariable("MSBuildExtensionsPath", "/usr/local/share/dotnet/sdk/11.0.100");
        Environment.SetEnvironmentVariable("MSBuildSDKsPath", "/usr/local/share/dotnet/sdk/11.0.100/Sdks");
        Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", "/usr/local/share/dotnet/sdk/11.0.100/MSBuild.dll");
        Environment.SetEnvironmentVariable(UnrelatedVariable, "kept");
    }

    public void Dispose()
    {
        foreach (var (name, value) in _original)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    [Theory(DisplayName = "CreateProcessStartInfo_AppStartedByTheDotnetCli_PassesOnNoMSBuildSdkVariable")]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateProcessStartInfo_AppStartedByTheDotnetCli_PassesOnNoMSBuildSdkVariable(bool useShell)
    {
        var command = new ShellCommand { Command = "dotnet", UseShell = useShell }.WithArgumentList("build");

        var startInfo = _executor.CreateProcessStartInfo(command);

        foreach (var name in SdkVariables)
        {
            startInfo.Environment.ShouldNotContainKey(name);
        }
    }

    [Theory(DisplayName = "CreateProcessStartInfo_AppStartedByTheDotnetCli_PassesOnOtherVariables")]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateProcessStartInfo_AppStartedByTheDotnetCli_PassesOnOtherVariables(bool useShell)
    {
        var command = new ShellCommand { Command = "dotnet", UseShell = useShell }.WithArgumentList("build");

        var startInfo = _executor.CreateProcessStartInfo(command);

        startInfo.Environment[UnrelatedVariable].ShouldBe("kept");
    }

    [Fact(DisplayName = "CreateProcessStartInfo_CommandSetsAnMSBuildSdkVariable_KeepsItsValue")]
    public void CreateProcessStartInfo_CommandSetsAnMSBuildSdkVariable_KeepsItsValue()
    {
        var command = new ShellCommand { Command = "dotnet" }
            .WithArgumentList("build")
            .WithoutShell()
            .WithEnvironment(new Dictionary<string, string> { ["MSBuildSDKsPath"] = "/chosen/Sdks" });

        _executor.CreateProcessStartInfo(command).Environment["MSBuildSDKsPath"].ShouldBe("/chosen/Sdks");
    }

    [Fact(DisplayName = "CreateProcessStartInfo_ShellCommandSetsAnMSBuildSdkVariable_ExportsItsValue")]
    public void CreateProcessStartInfo_ShellCommandSetsAnMSBuildSdkVariable_ExportsItsValue()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows runs shell commands directly");

        var command = new ShellCommand { Command = "dotnet" }
            .WithArgumentList("build")
            .WithEnvironment(new Dictionary<string, string> { ["MSBuildSDKsPath"] = "/chosen/Sdks" });

        _executor.CreateProcessStartInfo(command).ArgumentList[^1].ShouldContain("export MSBuildSDKsPath='/chosen/Sdks';");
    }
}
