using Mythetech.Framework.Desktop.Services;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

public class LoginEnvironmentTests
{
    private const string Marker = "__MARK__";

    [Fact(DisplayName = "Parse_ProfileOutputAroundMarkers_ReadsOnlyTheEnvironment")]
    public void Parse_ProfileOutputAroundMarkers_ReadsOnlyTheEnvironment()
    {
        var output = "Welcome back!\nLast login: today\n" +
                     Marker +
                     "PATH=/opt/homebrew/bin:/usr/bin\0MULTI=line1\nline2\0EQ=a=b\0EMPTY=\0" +
                     Marker;

        var environment = LoginEnvironment.Parse(output, Marker);

        environment.ShouldNotBeNull();
        environment.ShouldBe(new Dictionary<string, string>
        {
            ["PATH"] = "/opt/homebrew/bin:/usr/bin",
            ["MULTI"] = "line1\nline2",
            ["EQ"] = "a=b",
            ["EMPTY"] = "",
        }, ignoreOrder: true);
    }

    [Theory(DisplayName = "Parse_NoEnvironmentBetweenMarkers_ReturnsNull")]
    [InlineData("the profile exited before printing anything")]
    [InlineData("__MARK__")]
    [InlineData("__MARK____MARK__")]
    public void Parse_NoEnvironmentBetweenMarkers_ReturnsNull(string output)
    {
        LoginEnvironment.Parse(output, Marker).ShouldBeNull();
    }

    [Fact(DisplayName = "Changes_LoginAgainstInherited_KeepsOnlyWhatTheProfileSetOrChanged")]
    public void Changes_LoginAgainstInherited_KeepsOnlyWhatTheProfileSetOrChanged()
    {
        var login = new Dictionary<string, string>
        {
            ["PATH"] = "/opt/homebrew/bin:/usr/bin",
            ["HOME"] = "/Users/me",
            ["JAVA_HOME"] = "/opt/java",
            ["PWD"] = "/Users/me",
            ["OLDPWD"] = "/",
            ["SHLVL"] = "2",
            ["_"] = "/usr/bin/env",
        };
        var inherited = new Dictionary<string, string>
        {
            ["PATH"] = "/usr/bin",
            ["HOME"] = "/Users/me",
            ["TMPDIR"] = "/tmp",
        };

        LoginEnvironment.Changes(login, inherited).ShouldBe(new Dictionary<string, string>
        {
            ["PATH"] = "/opt/homebrew/bin:/usr/bin",
            ["JAVA_HOME"] = "/opt/java",
        }, ignoreOrder: true);
    }
}
