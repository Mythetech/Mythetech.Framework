using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Shell;
using Mythetech.Framework.WebAssembly.Shell;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

public class WasmShellExecutorArgumentTests
{
    [Fact(DisplayName = "ArgumentList_RegisteredHandler_ReceivesValuesVerbatim")]
    public async Task ArgumentList_RegisteredHandler_ReceivesValuesVerbatim()
    {
        string[]? received = null;
        var registry = new CommandRegistry();
        registry.RegisterSync("echo", args =>
        {
            received = args;
            return new ShellResult { ExitCode = 0 };
        });
        var executor = new WasmShellExecutor(Substitute.For<IJSRuntime>(), registry, new WasmShellOptions());
        string[] values = ["with space", "it's", "say \"hi\"", ""];

        await executor.ExecuteAsync(
            new ShellCommand { Command = "echo" }.WithArgumentList(values),
            TestContext.Current.CancellationToken);

        received.ShouldBe(values);
    }
}
