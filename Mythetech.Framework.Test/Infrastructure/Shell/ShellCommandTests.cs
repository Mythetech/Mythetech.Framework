using System.Diagnostics;
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

    [Fact(DisplayName = "OwnProcessGroup_DefaultsToFalse")]
    public void OwnProcessGroup_DefaultsToFalse()
    {
        new ShellCommand { Command = "git" }.OwnProcessGroup.ShouldBeFalse();
    }

    [Fact(DisplayName = "WithOwnProcessGroup_SetsTheFlag")]
    public void WithOwnProcessGroup_SetsTheFlag()
    {
        new ShellCommand { Command = "git" }.WithOwnProcessGroup().OwnProcessGroup.ShouldBeTrue();
    }

    [Fact(DisplayName = "IShellProcess_ImplementerWithoutTheNewMembers_GetsTheDefaults")]
    public void IShellProcess_ImplementerWithoutTheNewMembers_GetsTheDefaults()
    {
        IShellProcess process = new MinimalShellProcess(Environment.ProcessId);
        using var current = Process.GetCurrentProcess();
        DateTimeOffset osStartedAt = current.StartTime.ToUniversalTime();

        process.OwnsProcessGroup.ShouldBeFalse();
        (process.StartedAt - osStartedAt).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "StartedAt_DefaultForAProcessThatIsGone_IsMinValue")]
    public void StartedAt_DefaultForAProcessThatIsGone_IsMinValue()
    {
        IShellProcess process = new MinimalShellProcess(int.MaxValue);

        process.StartedAt.ShouldBe(DateTimeOffset.MinValue);
    }

    private sealed class MinimalShellProcess(int processId) : IShellProcess
    {
        public int ProcessId => processId;
        public bool HasExited => false;
        public Stream StandardInput => Stream.Null;
        public event Action<string>? OutputReceived { add { } remove { } }
        public event Action<string>? ErrorReceived { add { } remove { } }
        public event Action<int>? Exited { add { } remove { } }
        public Task WriteInputAsync(string input, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> WaitForExitAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public void Interrupt() { }
        public void Kill(bool entireProcessTree = true) { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
