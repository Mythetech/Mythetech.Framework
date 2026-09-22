using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

public class ShellCommandTests
{
    [Fact(DisplayName = "WithArgumentList_SetsArgumentsAsGiven")]
    public void WithArgumentList_SetsArgumentsAsGiven()
    {
        var command = new ShellCommand { Command = "git" }.WithArgumentList("commit", "-m", "it's done");

        command.ArgumentList.ShouldBe(["commit", "-m", "it's done"]);
        command.Arguments.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ArgumentList_DefaultsToNull")]
    public void ArgumentList_DefaultsToNull()
    {
        new ShellCommand { Command = "git" }.ArgumentList.ShouldBeNull();
    }

    [Fact(DisplayName = "ArgumentList_WithArgumentsAlreadySet_Throws")]
    public void ArgumentList_WithArgumentsAlreadySet_Throws()
    {
        var command = new ShellCommand { Command = "git", Arguments = "status" };

        Should.Throw<ArgumentException>(() => command.WithArgumentList("status"));
    }

    [Fact(DisplayName = "Arguments_WithArgumentListAlreadySet_Throws")]
    public void Arguments_WithArgumentListAlreadySet_Throws()
    {
        var command = new ShellCommand { Command = "git", ArgumentList = ["status"] };

        Should.Throw<ArgumentException>(() => command.WithArguments("status"));
    }

    [Fact(DisplayName = "ArgumentList_WithEmptyArguments_IsAllowed")]
    public void ArgumentList_WithEmptyArguments_IsAllowed()
    {
        var command = new ShellCommand { Command = "git", Arguments = "", ArgumentList = ["status"] };

        command.ArgumentList.ShouldBe(["status"]);
    }
}
